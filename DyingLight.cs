using System.Globalization;
using Applib;
using zlib;
using System.Text;

namespace DyingLight
{
    public class DyingLight
    {
        public class RP6LHeader
        {
            public string Signature = "";
            public uint Version;
            public uint CompressionMethod;
            public uint PartAmount;
            public uint SectionAmount;
            public uint FileAmount;
            public uint FilenameChunkLength;
            public uint FilenameAmount;
            public uint BlockSize;
        }

        public class RP6LSectionInfo
        {
            public byte Filetype;
            public byte Unknown1;
            public byte Unknown2;
            public byte Unknown3;
            public uint Offset;
            public uint UnpackedSize;
            public uint PackedSize;
            public uint Unknown4;
        }

        public class RP6LPartInfo
        {
            public byte SectionIndex;
            public byte Unknown1;
            public ushort FileIndex;
            public uint Offset;
            public uint Size;
            public uint Unknown2;
        }

        public class RP6LFileInfo
        {
            public byte PartAmount;
            public byte Unknown1;
            public byte Filetype;
            public byte Unknown2;
            public uint FileIndex;
            public uint FirstPart;
        }

        public static Dictionary<byte, string> ResourceTypeLookup = new Dictionary<byte, string>
        {
            {0x10, "mesh"}, {0x12, "skin"}, {0x20, "texture"}, {0x30, "material"},
            {0x40, "animation"}, {0x41, "animation_id"}, {0x42, "animation_scr"},
            {0x50, "fx"}, {0x60, "lightmap"}, {0x61, "flash"}, {0x65, "sound"},
            {0x66, "sound_music"}, {0x67, "sound_speech"}, {0x68, "sound_stream"},
            {0x69, "sound_local"}, {0x70, "density_map"}, {0x80, "height_map"},
            {0x90, "mimic"}, {0xA0, "pathmap"}, {0xB0, "phonemes"}, {0xC0, "static_geometry"},
            {0xD0, "text"}, {0xE0, "binary"}, {0xF8, "tiny_objects"}, {0xFF, "resource_list"}
        };

        public static HashSet<byte> DesiredTypes = new HashSet<byte> { 0x10, 0x20, 0x40 };

        public static bool ExtractRP6L(string path)
        {
            try
            {
                using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                using BinaryReader reader = new BinaryReader(fileStream);

                byte[] signatureBytes = reader.ReadBytes(4);
                string signature = Encoding.ASCII.GetString(signatureBytes);

                if (signature != "RP6L")
                {
                    Console.WriteLine($"无效文件:{path}");
                    return false;
                }

                RP6LHeader header = new RP6LHeader
                {
                    Signature = signature,
                    Version = reader.ReadUInt32(),
                    CompressionMethod = reader.ReadUInt32(),
                    PartAmount = reader.ReadUInt32(),
                    SectionAmount = reader.ReadUInt32(),
                    FileAmount = reader.ReadUInt32(),
                    FilenameChunkLength = reader.ReadUInt32(),
                    FilenameAmount = reader.ReadUInt32(),
                    BlockSize = reader.ReadUInt32()
                };

                Console.WriteLine($"解包:{path}");

                uint offsetMultiplier = header.Version == 4 ? 16u : 1u;

                List<RP6LSectionInfo> sectionInfos = new List<RP6LSectionInfo>();
                for (int i = 0; i < header.SectionAmount; i++)
                {
                    RP6LSectionInfo sectionInfo = new RP6LSectionInfo
                    {
                        Filetype = reader.ReadByte(),
                        Unknown1 = reader.ReadByte(),
                        Unknown2 = reader.ReadByte(),
                        Unknown3 = reader.ReadByte(),
                        Offset = reader.ReadUInt32(),
                        UnpackedSize = reader.ReadUInt32(),
                        PackedSize = reader.ReadUInt32(),
                        Unknown4 = reader.ReadUInt32()
                    };
                    sectionInfos.Add(sectionInfo);
                }

                List<RP6LPartInfo> partInfos = new List<RP6LPartInfo>();
                for (int i = 0; i < header.PartAmount; i++)
                {
                    RP6LPartInfo partInfo = new RP6LPartInfo
                    {
                        SectionIndex = reader.ReadByte(),
                        Unknown1 = reader.ReadByte(),
                        FileIndex = reader.ReadUInt16(),
                        Offset = reader.ReadUInt32(),
                        Size = reader.ReadUInt32(),
                        Unknown2 = reader.ReadUInt32()
                    };
                    partInfos.Add(partInfo);
                }

                List<RP6LFileInfo> fileInfos = new List<RP6LFileInfo>();
                for (int i = 0; i < header.FileAmount; i++)
                {
                    RP6LFileInfo fileInfo = new RP6LFileInfo
                    {
                        PartAmount = reader.ReadByte(),
                        Unknown1 = reader.ReadByte(),
                        Filetype = reader.ReadByte(),
                        Unknown2 = reader.ReadByte(),
                        FileIndex = reader.ReadUInt32(),
                        FirstPart = reader.ReadUInt32()
                    };
                    fileInfos.Add(fileInfo);
                }

                List<uint> filenameOffsets = new List<uint>();
                for (int i = 0; i < header.FileAmount; i++)
                {
                    filenameOffsets.Add(reader.ReadUInt32());
                }

                byte[] filenameChunk = reader.ReadBytes((int)header.FilenameChunkLength);
                string filenameChunkStr = Encoding.ASCII.GetString(filenameChunk);

                string? directoryName = Path.GetDirectoryName(path);
                string extractFolder = Path.Combine(directoryName ?? Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(path) + "_extracted");
                Directory.CreateDirectory(extractFolder);

                Dictionary<int, byte[]> sectionFiles = new Dictionary<int, byte[]>();

                for (int i = 0; i < sectionInfos.Count; i++)
                {
                    RP6LSectionInfo sectionInfo = sectionInfos[i];
                    if (sectionInfo.PackedSize > 0)
                    {
                        fileStream.Seek(sectionInfo.Offset * offsetMultiplier, SeekOrigin.Begin);
                        byte[] compressedData = reader.ReadBytes((int)sectionInfo.PackedSize);
                        byte[] uncompressedData = InflateData(compressedData);
                        sectionFiles[i] = uncompressedData;
                    }
                }

                int extractedCount = 0;

                for (int i = 0; i < fileInfos.Count; i++)
                {
                    RP6LFileInfo fileInfo = fileInfos[i];
                    if (!DesiredTypes.Contains(fileInfo.Filetype))
                        continue;

                    string resourceType = ResourceTypeLookup.ContainsKey(fileInfo.Filetype) ? ResourceTypeLookup[fileInfo.Filetype] : "unknown";

                    uint filenameOffset = filenameOffsets[i];
                    int filenameEnd = filenameChunkStr.IndexOf('\0', (int)filenameOffset);
                    string filename = filenameEnd == -1
                        ? filenameChunkStr.Substring((int)filenameOffset)
                        : filenameChunkStr.Substring((int)filenameOffset, filenameEnd - (int)filenameOffset);

                    string typeFolder = Path.Combine(extractFolder, resourceType);
                    Directory.CreateDirectory(typeFolder);

                    List<byte> fileData = new List<byte>();
                    uint currentPart = fileInfo.FirstPart;

                    for (int partIndex = 0; partIndex < fileInfo.PartAmount; partIndex++)
                    {
                        if (currentPart >= partInfos.Count)
                            break;

                        RP6LPartInfo partInfo = partInfos[(int)currentPart];
                        int sectionIndex = partInfo.SectionIndex;
                        uint dataLength = partInfo.Size;

                        byte[] partData;
                        if (sectionFiles.ContainsKey(sectionIndex))
                        {
                            byte[] sectionData = sectionFiles[sectionIndex];
                            uint dataOffset = partInfo.Offset;
                            if (dataOffset + dataLength <= sectionData.Length)
                            {
                                partData = new byte[dataLength];
                                Array.Copy(sectionData, dataOffset, partData, 0, dataLength);
                            }
                            else
                            {
                                partData = Array.Empty<byte>();
                            }
                        }
                        else
                        {
                            RP6LSectionInfo sectionInfoObj = sectionInfos[sectionIndex];
                            long textureDataOffset = (sectionInfoObj.Offset + partInfo.Offset) * offsetMultiplier;
                            fileStream.Seek(textureDataOffset, SeekOrigin.Begin);
                            partData = reader.ReadBytes((int)dataLength);
                        }

                        fileData.AddRange(partData);
                        currentPart++;
                    }

                    byte[] rawData = fileData.ToArray();
                    string targetPath;

                    if (fileInfo.Filetype == 0x20)
                    {
                        filename += ".dds";
                        targetPath = Path.Combine(typeFolder, filename);
                        CreateDDSFileLegacy(rawData, targetPath);
                    }
                    else if (fileInfo.Filetype == 0x10)
                    {
                        filename += ".msh";
                        targetPath = Path.Combine(typeFolder, filename);
                        File.WriteAllBytes(targetPath, rawData);
                    }
                    else if (fileInfo.Filetype == 0x40)
                    {
                        filename += ".anm";
                        targetPath = Path.Combine(typeFolder, filename);
                        File.WriteAllBytes(targetPath, rawData);
                    }
                    else
                    {
                        targetPath = Path.Combine(typeFolder, filename);
                        File.WriteAllBytes(targetPath, rawData);
                    }

                    Console.WriteLine($"提取:{resourceType}/{Path.GetFileName(targetPath)}");
                    extractedCount++;
                }

                Console.WriteLine($"完成:{extractFolder}(提取了{extractedCount}个文件)");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"失败{path}:{e}");
                return false;
            }
        }

        private static void CreateDDSFileLegacy(byte[] textureData, string outputPath)
        {
            try
            {
                using MemoryStream textureStream = new MemoryStream(textureData);
                using BinaryReader textureReader = new BinaryReader(textureStream);
                using FileStream fileStream2 = new FileStream(outputPath, FileMode.Create);
                using BinaryWriter binaryWriter = new BinaryWriter(fileStream2);

                int width = textureReader.ReadInt16();
                int height = textureReader.ReadInt16();
                textureReader.ReadInt16();
                textureReader.ReadInt16();
                textureReader.ReadInt16();
                textureReader.ReadInt16();
                int format = textureReader.ReadInt32();
                textureReader.ReadInt32();
                textureReader.ReadInt16();
                textureReader.ReadByte();
                textureReader.ReadInt32();
                int dataSize = textureReader.ReadInt32();

                int ddsFormat = 808540228;

                if (format == 2)
                {
                    format = 28;
                }
                else if (format == 14)
                {
                    format = 61;
                }
                else if (format == 17)
                {
                    ddsFormat = 827611204;
                }
                else if (format == 18)
                {
                    ddsFormat = 861165636;
                }
                else if (format == 19)
                {
                    ddsFormat = 894720068;
                }
                else if (format == 33)
                {
                    format = 10;
                }
                else
                {
                    Console.WriteLine("未知的纹理格式" + format);
                }

                binaryWriter.Write(533118272580L);
                binaryWriter.Write(4103);
                binaryWriter.Write(height);
                binaryWriter.Write(width);
                binaryWriter.Write(dataSize);
                binaryWriter.Write(0);
                binaryWriter.Write(1);
                fileStream2.Seek(44L, SeekOrigin.Current);
                binaryWriter.Write(32);
                binaryWriter.Write(4);
                binaryWriter.Write(ddsFormat);
                fileStream2.Seek(40L, SeekOrigin.Current);
                if (ddsFormat == 808540228)
                {
                    binaryWriter.Write(format);
                    binaryWriter.Write(3);
                    binaryWriter.Write(0);
                    binaryWriter.Write(1);
                    binaryWriter.Write(0);
                }

                byte[] textureRawData = new byte[dataSize];
                Array.Copy(textureData, textureData.Length - dataSize, textureRawData, 0, dataSize);
                fileStream2.Write(textureRawData, 0, dataSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建DDS文件失败{outputPath}:{ex.Message}");
                File.WriteAllBytes(outputPath, textureData);
            }
        }
        public static byte[] InflateData(byte[] compressedData)
        {
            using MemoryStream output = new MemoryStream();
            using ZOutputStream zoutput = new ZOutputStream(output);
            zoutput.Write(compressedData, 0, compressedData.Length);
            zoutput.Flush();
            return output.ToArray();
        }
        public static Quaternion3D matrix2quat(float[,] m)
        {
            int[] array = new int[3];
            array[0] = 1;
            array[1] = 2;
            int[] array2 = array;
            Quaternion3D quaternion3D = new Quaternion3D();
            double[] array3 = new double[4];
            double num = (double)(m[0, 0] + m[1, 1] + m[2, 2]);
            if (num > 0.0)
            {
                double num2 = Math.Pow(num + 1.0, 0.5);
                quaternion3D.real = (float)(num2 / 2.0);
                num2 = 0.5 / num2;
                quaternion3D.i = (float)((double)(m[1, 2] - m[2, 1]) * num2);
                quaternion3D.j = (float)((double)(m[2, 0] - m[0, 2]) * num2);
                quaternion3D.k = (float)((double)(m[0, 1] - m[1, 0]) * num2);
            }
            else
            {
                int num3 = 0;
                if (m[1, 1] > m[0, 0])
                {
                    num3 = 1;
                }
                if (m[2, 2] > m[num3, num3])
                {
                    num3 = 2;
                }
                int num4 = array2[num3];
                int num5 = array2[num4];
                double num2 = Math.Pow((double)(m[num3, num3] - (m[num4, num4] + m[num5, num5])) + 1.0, 0.5);
                array3[num3] = num2 * 0.5;
                if (num2 != 0.0)
                {
                    num2 = 0.5 / num2;
                }
                array3[3] = (double)(m[num4, num5] - m[num5, num4]) * num2;
                array3[num4] = (double)(m[num3, num4] + m[num4, num3]) * num2;
                array3[num5] = (double)(m[num3, num5] + m[num5, num3]) * num2;
                quaternion3D.i = (float)array3[0];
                quaternion3D.j = (float)array3[1];
                quaternion3D.k = (float)array3[2];
                quaternion3D.real = (float)array3[3];
            }
            return quaternion3D;
        }
        public static string readname(BinaryReader br)
        {
            string text = "";
            byte b;
            while ((b = br.ReadByte()) > 0)
            {
                text += (char)b;
            }
            return text;
        }
        public static float readhalf(BinaryReader br)
        {
            uint num = (uint)br.ReadUInt16();
            byte[] array = new byte[4];
            uint num2 = (num & 32768U) >> 8;
            uint num3 = num & 31744U;
            if (num3 > 0U)
            {
                num3 = num3 + 114688U >> 3;
            }
            uint num4 = (num & 1023U) << 5;
            array[3] = (byte)(num2 | ((num3 >> 8) & 255U));
            array[2] = (byte)((num3 & 128U) | ((num4 >> 8) & 127U));
            array[1] = (byte)(num4 & 255U);
            array[0] = 0;
            return BitConverter.ToSingle(array, 0);
        }

        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("用法:DyingLight <rpack文件>");
                Console.WriteLine("请提供一个文件作为参数");
                return;
            }

            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"错误:文件未找到:{filePath}");
                return;
            }

            string extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".rpack")
            {
                ExtractRP6L(filePath);
            }
            else
            {
                ProcessLegacyFormat(filePath);
            }
        }

        public static void ProcessLegacyFormat(string filePath)
        {
            try
            {
                string? dirName = Path.GetDirectoryName(filePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                dirName = dirName ?? Environment.CurrentDirectory;
                string outputDir = Path.Combine(dirName, fileNameWithoutExt);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                NumberFormatInfo numberFormatInfo = new NumberFormatInfo();
                numberFormatInfo.NumberDecimalSeparator = ".";
                float num = 0f;
                float num2 = 0f;
                float num3 = 0f;
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                BinaryReader binaryReader = new BinaryReader(fileStream);
                Console.WriteLine(filePath);
                Console.WriteLine("------------------------------");
                binaryReader.ReadInt32();
                binaryReader.ReadInt32();
                binaryReader.ReadInt32();
                int num4 = binaryReader.ReadInt32();
                int num5 = binaryReader.ReadInt32();
                int num6 = binaryReader.ReadInt32();
                int num7 = binaryReader.ReadInt32();
                int num8 = binaryReader.ReadInt32();
                binaryReader.ReadInt32();
                if (num5 <= 0)
                {
                    Console.WriteLine("错误:元素数量无效(num5)");
                    return;
                }
                if (num8 != num6)
                {
                    Console.WriteLine("Names != assets");
                }
                int[] array = new int[num5];
                uint[] array2 = new uint[num5];
                uint[] array3 = new uint[num5];
                int[] array4 = new int[num5];
                for (int i = 0; i < num5; i++)
                {
                    if (fileStream.Position >= fileStream.Length - 12)
                    {
                        Console.WriteLine($"警告:读取元素时到达文件末尾{i}");
                        break;
                    }
                    array[i] = (int)binaryReader.ReadInt16();
                    binaryReader.ReadInt16();
                    array2[i] = binaryReader.ReadUInt32();
                    array3[i] = binaryReader.ReadUInt32();
                    array4[i] = binaryReader.ReadInt32();
                    binaryReader.ReadInt16();
                    binaryReader.ReadInt16();
                }
                int[] array5 = new int[num4];
                uint[] array6 = new uint[num4];
                int[] array7 = new int[num4];
                for (int i = 0; i < num4; i++)
                {
                    array5[i] = (int)binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadInt16();
                    array6[i] = binaryReader.ReadUInt32();
                    array7[i] = binaryReader.ReadInt32();
                    binaryReader.ReadInt32();
                }
                int[] array8 = new int[num6];
                int[] array9 = new int[num6];
                int[] array10 = new int[num6];
                int[] array11 = new int[num6];
                for (int i = 0; i < num6; i++)
                {
                    array8[i] = (int)binaryReader.ReadInt16();
                    array9[i] = (int)binaryReader.ReadInt16();
                    array10[i] = binaryReader.ReadInt32();
                    array11[i] = binaryReader.ReadInt32();
                }
                int[] array12 = new int[num8];
                for (int i = 0; i < num8; i++)
                {
                    array12[i] = binaryReader.ReadInt32();
                }
                byte[] array13 = new byte[num7];
                fileStream.Read(array13, 0, array13.Length);
                MemoryStream[] array14 = new MemoryStream[num5];
                for (int i = 0; i < num5; i++)
                {
                    if (array[i] != 33)
                    {
                        array14[i] = new MemoryStream();
                        fileStream.Seek((long)((ulong)array2[i]), SeekOrigin.Begin);
                        if (array4[i] == 0)
                        {
                            byte[] array15 = new byte[array3[i]];
                            new ZOutputStream(array14[i]);
                            fileStream.Read(array15, 0, array15.Length);
                            array14[i].Write(array15, 0, array15.Length);
                        }
                        else
                        {
                            byte[] array16 = new byte[array4[i]];
                            ZOutputStream zoutputStream = new ZOutputStream(array14[i]);
                            fileStream.Read(array16, 0, array16.Length);
                            zoutputStream.Write(array16, 0, array16.Length);
                            zoutputStream.Flush();
                        }
                    }
                }
                MemoryStream memoryStream = new MemoryStream(array13);
                BinaryReader binaryReader2 = new BinaryReader(memoryStream);
                for (int j = 0; j < num6; j++)
                {
                    memoryStream.Seek((long)array12[array10[j]], SeekOrigin.Begin);
                    string text = DyingLight.readname(binaryReader2);
                    Console.WriteLine(array9[j].ToString("X") + "\t" + text);
                    if (array9[j] == 272)
                    {
                        if (array8[j] != 5)
                        {
                            Console.WriteLine("模型不支持的块计数 = " + array8[j]);
                        }
                        else
                        {
                            int num9 = array11[j];
                            MemoryStream memoryStream2 = array14[array5[num9]];
                            BinaryReader binaryReader3 = new BinaryReader(memoryStream2);
                            uint num10 = array6[num9];
                            MemoryStream memoryStream3 = array14[array5[num9 + 3]];
                            BinaryReader binaryReader4 = new BinaryReader(memoryStream3);
                            uint num11 = array6[num9 + 3];
                            MemoryStream memoryStream4 = array14[array5[num9 + 4]];
                            BinaryReader binaryReader5 = new BinaryReader(memoryStream4);
                            uint num12 = array6[num9 + 4];
                            memoryStream2.Seek((long)((ulong)(num10 + 8U)), SeekOrigin.Begin);
                            long num13 = (long)((ulong)num10 + (ulong)((long)binaryReader3.ReadInt32()) - 1UL);
                            memoryStream2.Seek((long)((ulong)(num10 + 80U)), SeekOrigin.Begin);
                            long num14 = (long)(binaryReader3.ReadInt32() - 1);
                            memoryStream2.Seek((long)((ulong)(num10 + 124U)), SeekOrigin.Begin);
                            int num15 = binaryReader3.ReadInt32();
                            bool[] array17 = new bool[num15];
                            long[] array18 = new long[num15];
                            int[] array19 = new int[num15];
                            int[] array20 = new int[num15];
                            int[] array21 = new int[num15];
                            int[] array22 = new int[num15];
                            int num16 = 0;
                            memoryStream2.Seek((long)((ulong)num10 + (ulong)num14), SeekOrigin.Begin);
                            for (int i = 0; i < num15; i++)
                            {
                                array21[i] = num16;
                                array18[i] = (long)(binaryReader3.ReadInt32() - 1);
                                binaryReader3.ReadInt32();
                                array19[i] = binaryReader3.ReadInt32();
                                binaryReader3.ReadInt32();
                                num16 += array19[i];
                                array22[i] = num16;
                            }
                            List<int> list = new List<int>();
                            List<int> list2 = new List<int>();
                            List<int> list3 = new List<int>();
                            for (int k = 0; k < num15; k++)
                            {
                                array17[k] = false;
                                memoryStream2.Seek((long)((ulong)num10 + (ulong)array18[k]), SeekOrigin.Begin);
                                for (int i = 0; i < array19[k]; i++)
                                {
                                    list.Add((int)binaryReader3.ReadByte());
                                    int num17 = (int)binaryReader3.ReadByte();
                                    list2.Add(num17);
                                    if (num17 == 5)
                                    {
                                        array20[k]++;
                                    }
                                    if (num17 == 1)
                                    {
                                        array17[k] = true;
                                    }
                                    list3.Add((int)binaryReader3.ReadByte());
                                    binaryReader3.ReadByte();
                                }
                            }
                            memoryStream2.Seek((long)((ulong)(num10 + 100U)), SeekOrigin.Begin);
                            int num18 = binaryReader3.ReadInt32();
                            int num19 = num18;
                            int[] array23 = new int[num19];
                            Vector3D[] array24 = new Vector3D[num19];
                            Quaternion3D[] array25 = new Quaternion3D[num19];
                            Vector3D[] array26 = new Vector3D[num19];
                            Quaternion3D[] array27 = new Quaternion3D[num19];
                            float[,] array28 = new float[3, 3];
                            string[] array29 = new string[num19];
                            int num20 = 0;
                            for (int i = 0; i < num18; i++)
                            {
                                memoryStream2.Seek(num13 + (long)(208 * i) + 120L, SeekOrigin.Begin);
                                int num21 = binaryReader3.ReadInt32();
                                if (num21 != 0)
                                {
                                    memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num21) - 1UL), SeekOrigin.Begin);
                                    string text2 = DyingLight.readname(binaryReader3);
                                    array29[i] = text2;
                                }
                                memoryStream2.Seek(num13 + (long)(208 * i), SeekOrigin.Begin);
                                array28[0, 0] = binaryReader3.ReadSingle();
                                array28[1, 0] = binaryReader3.ReadSingle();
                                array28[2, 0] = binaryReader3.ReadSingle();
                                num = binaryReader3.ReadSingle();
                                array28[0, 1] = binaryReader3.ReadSingle();
                                array28[1, 1] = binaryReader3.ReadSingle();
                                array28[2, 1] = binaryReader3.ReadSingle();
                                num2 = binaryReader3.ReadSingle();
                                array28[0, 2] = binaryReader3.ReadSingle();
                                array28[1, 2] = binaryReader3.ReadSingle();
                                array28[2, 2] = binaryReader3.ReadSingle();
                                num3 = binaryReader3.ReadSingle();
                                memoryStream2.Seek(num13 + (long)(208 * i) + 198L, SeekOrigin.Begin);
                                array23[i] = (int)binaryReader3.ReadInt16();
                                array24[i] = new Vector3D(num, num2, num3);
                                array25[i] = new Quaternion3D(DyingLight.matrix2quat(array28));
                                memoryStream2.Seek(num13 + (long)(208 * i) + 136L, SeekOrigin.Begin);
                                int num22 = binaryReader3.ReadInt32();
                                if (num22 != 0)
                                {
                                    num22--;
                                    memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num22) + 8UL), SeekOrigin.Begin);
                                    int num23 = binaryReader3.ReadInt32() - 1;
                                    memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num23) + 48UL), SeekOrigin.Begin);
                                    int num24 = (int)binaryReader3.ReadInt16();
                                    num20 += num24;
                                }
                            }
                            for (int i = 0; i < num19; i++)
                            {
                                array26 = new Vector3D[num19];
                                array27 = new Quaternion3D[num19];
                                for (i = 0; i < num19; i++)
                                {
                                    if (array23[i] < 0)
                                    {
                                        array26[i] = array24[i];
                                        array27[i] = array25[i];
                                    }
                                    else
                                    {
                                        int num25 = array23[i];
                                        array27[i] = array27[num25] * array25[i];
                                        Quaternion3D quaternion3D = new Quaternion3D(array24[i], 0f);
                                        Quaternion3D quaternion3D2 = array27[num25] * quaternion3D;
                                        Quaternion3D quaternion3D3 = quaternion3D2 * new Quaternion3D(array27[num25].real, -array27[num25].i, -array27[num25].j, -array27[num25].k);
                                        array26[i] = quaternion3D3.xyz;
                                        Vector3D[] array30;
                                        IntPtr intPtr;
                                        (array30 = array26)[(int)(intPtr = (IntPtr)i)] = array30[(int)intPtr] + array26[num25];
                                    }
                                }
                            }
                            if (!text.Contains("buildterrain"))
                            {
                                string modelsDir = Path.Combine(outputDir, "models");
                                if (!Directory.Exists(modelsDir))
                                {
                                    Directory.CreateDirectory(modelsDir);
                                }

                                StreamWriter streamWriter = new StreamWriter(Path.Combine(modelsDir, text + ".smd"));
                                streamWriter.WriteLine("version 1");
                                streamWriter.WriteLine("nodes");
                                for (int i = 0; i < num19; i++)
                                {
                                    streamWriter.WriteLine(string.Concat(new object[]
                                    {
                                    i,
                                    " \"",
                                    array29[i],
                                    "\" ",
                                    array23[i]
                                    }));
                                }
                                streamWriter.WriteLine("end");
                                streamWriter.WriteLine("skeleton");
                                streamWriter.WriteLine("time 0");
                                Vector3D vector3D = new Vector3D();
                                StreamWriter streamWriter2 = new StreamWriter(Path.Combine(modelsDir, text + ".ascii"));
                                streamWriter2.WriteLine(num19);
                                for (int i = 0; i < num19; i++)
                                {
                                    vector3D = C3D.ToEulerAngles(array25[i]);
                                    streamWriter.Write(i + "  ");
                                    streamWriter.Write(array24[i].X.ToString("0.000000", numberFormatInfo));
                                    streamWriter.Write(" " + array24[i].Y.ToString("0.000000", numberFormatInfo));
                                    streamWriter.Write(" " + array24[i].Z.ToString("0.000000", numberFormatInfo));
                                    streamWriter.Write("  " + vector3D.X.ToString("0.000000", numberFormatInfo));
                                    streamWriter.Write(" " + vector3D.Y.ToString("0.000000", numberFormatInfo));
                                    streamWriter.WriteLine(" " + vector3D.Z.ToString("0.000000", numberFormatInfo));
                                    streamWriter2.WriteLine(array29[i]);
                                    streamWriter2.WriteLine(array23[i]);
                                    streamWriter2.Write(array26[i].X.ToString("0.000000", numberFormatInfo));
                                    streamWriter2.Write(" " + array26[i].Y.ToString("0.000000", numberFormatInfo));
                                    streamWriter2.Write(" " + array26[i].Z.ToString("0.000000", numberFormatInfo));
                                    streamWriter2.WriteLine();
                                }
                                streamWriter.WriteLine("end");
                                streamWriter.Close();
                                streamWriter2.WriteLine(num20);
                                for (int i = 0; i < num18; i++)
                                {
                                    memoryStream2.Seek(num13 + (long)(208 * i) + 128L, SeekOrigin.Begin);
                                    memoryStream2.Seek(num13 + (long)(208 * i) + 136L, SeekOrigin.Begin);
                                    int num26 = binaryReader3.ReadInt32();
                                    if (num26 != 0)
                                    {
                                        num26--;
                                        memoryStream2.Seek(num13 + (long)(208 * i) + 202L, SeekOrigin.Begin);
                                        binaryReader3.ReadByte();
                                        memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num26) + 8UL), SeekOrigin.Begin);
                                        int num27 = binaryReader3.ReadInt32() - 1;
                                        binaryReader3.ReadInt32();
                                        int num28 = binaryReader3.ReadInt32() - 1;
                                        binaryReader3.ReadInt32();
                                        int num29 = binaryReader3.ReadInt32() - 1;
                                        binaryReader3.ReadInt32();
                                        memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num27)), SeekOrigin.Begin);
                                        int num30 = binaryReader3.ReadInt32() - 1;
                                        memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num27) + 24UL), SeekOrigin.Begin);
                                        uint num31 = num11 + binaryReader3.ReadUInt32();
                                        binaryReader3.ReadInt32();
                                        binaryReader3.ReadInt32();
                                        binaryReader3.ReadInt32();
                                        int num32 = binaryReader3.ReadInt32();
                                        uint num33 = num12 + binaryReader3.ReadUInt32();
                                        int num34 = (int)binaryReader3.ReadInt16();
                                        int num35 = (int)binaryReader3.ReadInt16();
                                        binaryReader3.ReadInt32();
                                        memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num29)), SeekOrigin.Begin);
                                        int[] array31 = new int[num34];
                                        int[][] array32 = new int[num34][];
                                        int[] array33 = new int[num34];
                                        for (int k = 0; k < num34; k++)
                                        {
                                            array33[k] = binaryReader3.ReadInt32();
                                            binaryReader3.ReadInt32();
                                            array31[k] = binaryReader3.ReadInt32();
                                            binaryReader3.ReadInt32();
                                        }
                                        for (int k = 0; k < num34; k++)
                                        {
                                            if (array33[k] > 0)
                                            {
                                                array32[k] = new int[array31[k]];
                                                memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)array33[k]) - 1UL), SeekOrigin.Begin);
                                                for (int l = 0; l < array31[k]; l++)
                                                {
                                                    array32[k][l] = (int)binaryReader3.ReadInt16();
                                                }
                                            }
                                        }
                                        memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num28)), SeekOrigin.Begin);
                                        Vector3D[] array34 = new Vector3D[num32];
                                        Vector3D[] array35 = new Vector3D[num32];
                                        float[,] array36 = new float[num32, 2];
                                        float[,] array37 = new float[num32, 2];
                                        int[,] array38 = new int[num32, 4];
                                        float[,] array39 = new float[num32, 4];
                                        memoryStream3.Seek((long)((ulong)num31), SeekOrigin.Begin);
                                        for (int k = 0; k < num32; k++)
                                        {
                                            for (int l = array21[num35]; l < array22[num35]; l++)
                                            {
                                                if (list2[l] == 0)
                                                {
                                                    if (list[l] == 2)
                                                    {
                                                        num = binaryReader4.ReadSingle();
                                                        num2 = binaryReader4.ReadSingle();
                                                        num3 = binaryReader4.ReadSingle();
                                                    }
                                                    else if (list[l] == 16)
                                                    {
                                                        num = DyingLight.readhalf(binaryReader4);
                                                        num2 = DyingLight.readhalf(binaryReader4);
                                                        num3 = DyingLight.readhalf(binaryReader4);
                                                        DyingLight.readhalf(binaryReader4);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的坐标类型" + list[l]);
                                                    }
                                                    array34[k] = new Vector3D(num, num2, num3);
                                                }
                                                else if (list2[l] == 1)
                                                {
                                                    if (list[l] == 4)
                                                    {
                                                        array39[k, 0] = (float)binaryReader4.ReadByte() / 255f;
                                                        array39[k, 1] = (float)binaryReader4.ReadByte() / 255f;
                                                        array39[k, 2] = (float)binaryReader4.ReadByte() / 255f;
                                                        array39[k, 3] = (float)binaryReader4.ReadByte() / 255f;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的坐标类型" + list[l]);
                                                    }
                                                }
                                                else if (list2[l] == 2)
                                                {
                                                    if (list[l] == 4)
                                                    {
                                                        array38[k, 0] = (int)binaryReader4.ReadByte();
                                                        array38[k, 1] = (int)binaryReader4.ReadByte();
                                                        array38[k, 2] = (int)binaryReader4.ReadByte();
                                                        array38[k, 3] = (int)binaryReader4.ReadByte();
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的坐标类型" + list[l]);
                                                    }
                                                }
                                                else if (list2[l] == 3)
                                                {
                                                    if (list[l] == 31)
                                                    {
                                                        num = (float)binaryReader4.ReadSByte() / 127f;
                                                        num2 = (float)binaryReader4.ReadSByte() / 127f;
                                                        num3 = (float)binaryReader4.ReadSByte() / 127f;
                                                        array35[k] = new Vector3D(num, num2, num3);
                                                        binaryReader4.ReadByte();
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的坐标类型" + list[l]);
                                                    }
                                                }
                                                else if (list2[l] == 6)
                                                {
                                                    if (list[l] == 31)
                                                    {
                                                        binaryReader4.ReadInt32();
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的切线类型" + list[l]);
                                                    }
                                                }
                                                else if (list2[l] == 5)
                                                {
                                                    if (list[l] == 15)
                                                    {
                                                        array36[k, list3[l]] = DyingLight.readhalf(binaryReader4);
                                                        array37[k, list3[l]] = DyingLight.readhalf(binaryReader4);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的UVs类型" + list[l]);
                                                    }
                                                }
                                                else if (list2[l] == 10)
                                                {
                                                    if (list[l] == 4)
                                                    {
                                                        binaryReader4.ReadByte();
                                                        binaryReader4.ReadByte();
                                                        binaryReader4.ReadByte();
                                                        binaryReader4.ReadByte();
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("未知的颜色类型" + list[l]);
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("未知的语义" + list2[l]);
                                                }
                                            }
                                        }
                                        memoryStream2.Seek((long)((ulong)num10 + (ulong)((long)num30)), SeekOrigin.Begin);
                                        for (int k = 0; k < num34; k++)
                                        {
                                            int num36 = binaryReader3.ReadInt32();
                                            int num37 = 0;
                                            int num38 = num32;
                                            memoryStream4.Seek((long)((ulong)num33), SeekOrigin.Begin);
                                            for (int l = 0; l < num36; l++)
                                            {
                                                int num39 = (int)binaryReader5.ReadUInt16();
                                                if (num39 > num37)
                                                {
                                                    num37 = num39;
                                                }
                                                if (num39 < num38)
                                                {
                                                    num38 = num39;
                                                }
                                            }
                                            streamWriter2.WriteLine(string.Concat(new object[]
                                            {
                                            "sm_",
                                            i,
                                            "_",
                                            array29[i],
                                            "_",
                                            k
                                            }));
                                            streamWriter2.WriteLine(array20[num35]);
                                            streamWriter2.WriteLine(0);
                                            streamWriter2.WriteLine(num37 - num38 + 1);
                                            for (int l = num38; l <= num37; l++)
                                            {
                                                streamWriter2.Write(array34[l].X.ToString("0.######", numberFormatInfo));
                                                streamWriter2.Write(" " + array34[l].Y.ToString("0.######", numberFormatInfo));
                                                streamWriter2.Write(" " + array34[l].Z.ToString("0.######", numberFormatInfo));
                                                streamWriter2.WriteLine();
                                                streamWriter2.Write(array35[l].X.ToString("0.######", numberFormatInfo));
                                                streamWriter2.Write(" " + array35[l].Y.ToString("0.######", numberFormatInfo));
                                                streamWriter2.Write(" " + array35[l].Z.ToString("0.######", numberFormatInfo));
                                                streamWriter2.WriteLine();
                                                streamWriter2.WriteLine("0 0 0 0");
                                                for (int m = 0; m < array20[num35]; m++)
                                                {
                                                    streamWriter2.WriteLine(array36[l, m].ToString("0.######", numberFormatInfo) + " " + array37[l, m].ToString("0.######", numberFormatInfo));
                                                }
                                                if (array17[num35])
                                                {
                                                    streamWriter2.Write(array32[k][array38[l, 0]] + " ");
                                                    streamWriter2.Write(array32[k][array38[l, 1]] + " ");
                                                    streamWriter2.Write(array32[k][array38[l, 2]] + " ");
                                                    streamWriter2.WriteLine(array32[k][array38[l, 3]]);
                                                    streamWriter2.Write(array39[l, 0].ToString("0.######", numberFormatInfo) + " ");
                                                    streamWriter2.Write(array39[l, 1].ToString("0.######", numberFormatInfo) + " ");
                                                    streamWriter2.Write(array39[l, 2].ToString("0.######", numberFormatInfo) + " ");
                                                    streamWriter2.WriteLine(array39[l, 3].ToString("0.######", numberFormatInfo));
                                                }
                                                else
                                                {
                                                    streamWriter2.WriteLine("0 0 0 0");
                                                    streamWriter2.WriteLine("1 0 0 0");
                                                }
                                            }
                                            streamWriter2.WriteLine(num36 / 3);
                                            memoryStream4.Seek((long)((ulong)num33), SeekOrigin.Begin);
                                            int[] array40 = new int[3];
                                            for (int l = 0; l < num36 / 3; l++)
                                            {
                                                for (int n = 0; n < 3; n++)
                                                {
                                                    array40[n] = (int)binaryReader5.ReadUInt16();
                                                }
                                                streamWriter2.Write(array40[2] - num38);
                                                streamWriter2.Write(" " + (array40[1] - num38));
                                                streamWriter2.Write(" " + (array40[0] - num38));
                                                streamWriter2.WriteLine();
                                            }
                                            num33 += (uint)(num36 * 2);
                                        }
                                    }
                                }
                                streamWriter2.Close();
                            }
                        }
                    }
                    else if (array9[j] == 8480)
                    {
                        int num40 = array11[j];
                        MemoryStream memoryStream2 = array14[array5[num40]];
                        BinaryReader binaryReader3 = new BinaryReader(memoryStream2);
                        uint num41 = array6[num40];
                        memoryStream2.Seek((long)((ulong)num41), SeekOrigin.Begin);
                        int num42 = (int)binaryReader3.ReadInt16();
                        int num43 = (int)binaryReader3.ReadInt16();
                        binaryReader3.ReadInt16();
                        binaryReader3.ReadInt16();
                        binaryReader3.ReadInt16();
                        binaryReader3.ReadInt16();
                        int num44 = binaryReader3.ReadInt32();
                        binaryReader3.ReadInt32();
                        binaryReader3.ReadInt16();
                        binaryReader3.ReadByte();
                        binaryReader3.ReadInt32();
                        int num45 = binaryReader3.ReadInt32();
                        int num46 = 808540228;
                        if (num44 == 2)
                        {
                            num44 = 28;
                        }
                        else if (num44 == 14)
                        {
                            num44 = 61;
                        }
                        else if (num44 == 17)
                        {
                            num46 = 827611204;
                        }
                        else if (num44 == 18)
                        {
                            num46 = 861165636;
                        }
                        else if (num44 == 19)
                        {
                            num46 = 894720068;
                        }
                        else if (num44 == 33)
                        {
                            num44 = 10;
                        }
                        else
                        {
                            Console.WriteLine("未知的纹理格式" + num44);
                        }
                        text += ".dds";
                        if (array8[j] == 2)
                        {
                            num45 = array7[num40 + 1];
                        }

                        string texturesDir = Path.Combine(outputDir, "textures");
                        if (!Directory.Exists(texturesDir))
                        {
                            Directory.CreateDirectory(texturesDir);
                        }

                        FileStream fileStream2 = new FileStream(Path.Combine(texturesDir, text), FileMode.Create);
                        BinaryWriter binaryWriter = new BinaryWriter(fileStream2);
                        binaryWriter.Write(533118272580L);
                        binaryWriter.Write(4103);
                        binaryWriter.Write(num43);
                        binaryWriter.Write(num42);
                        binaryWriter.Write(num45);
                        binaryWriter.Write(0);
                        binaryWriter.Write(1);
                        fileStream2.Seek(44L, SeekOrigin.Current);
                        binaryWriter.Write(32);
                        binaryWriter.Write(4);
                        binaryWriter.Write(num46);
                        fileStream2.Seek(40L, SeekOrigin.Current);
                        if (num46 == 808540228)
                        {
                            binaryWriter.Write(num44);
                            binaryWriter.Write(3);
                            binaryWriter.Write(0);
                            binaryWriter.Write(1);
                            binaryWriter.Write(0);
                        }
                        if (array8[j] > 2)
                        {
                            int num47 = array5[num40 + 1];
                            byte[] array41 = new byte[num45];
                            fileStream.Seek((long)((ulong)(array2[num47] + array6[num40 + 1])), SeekOrigin.Begin);
                            fileStream.Read(array41, 0, num45);
                            fileStream2.Write(array41, 0, num45);
                        }
                        else
                        {
                            MemoryStream memoryStream3 = array14[array5[num40 + 1]];
                            uint num48 = array6[num40 + 1];
                            byte[] array42 = new byte[num45];
                            memoryStream3.Seek((long)((ulong)num48), SeekOrigin.Begin);
                            memoryStream3.Read(array42, 0, num45);
                            fileStream2.Write(array42, 0, num45);
                        }
                        binaryWriter.Close();
                        fileStream2.Close();
                    }
                }

                Console.WriteLine($"解包完成!文件已输出到:{outputDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理文件时出错:{ex.Message}");
                Console.WriteLine($"堆栈跟踪:{ex.StackTrace}");
            }
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETSTANDARD
#else
using PCLStorage;
#endif

namespace P42.Utils
{
    public class TTFAnalyzer
    {

        // Font file; must be seekable
        //RandomAccessFile m_file;
        Stream _stream;

        // Helper I/O functions
        int ReadByte()
        {
            return _stream.ReadByte() & 0xFF;
            //return m_file.Read() & 0xFF;
        }

        int ReadWord()
        {
            int b1 = ReadByte();
            int b2 = ReadByte();
            return b1 << 8 | b2;
        }

        int ReadDword()
        {
            int b1 = ReadByte();
            int b2 = ReadByte();
            int b3 = ReadByte();
            int b4 = ReadByte();
            return b1 << 24 | b2 << 16 | b3 << 8 | b4;
        }

        void Read(byte[] array)
        {
            if (_stream.Read(array, 0, array.Length) != array.Length)
                throw new IOException();
        }

        // Helper
        int GetWord(byte[] array, int offset)
        {
            int b1 = array[offset] & 0xFF;
            int b2 = array[offset + 1] & 0xFF;
            return b1 << 8 | b2;
        }
        
        void Seek(int offset)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
        }


#if NETSTANDARD
        public static string FontFamily(string fontFilePath)
        {
            String result = null;
            using (var stream = System.IO.File.Open(fontFilePath, System.IO.FileMode.Open))
            {
                result = FontFamily(stream);
            }
            return result;
        }
#else
        public static string FontFamily(IFile fontFile)
        {
            using(var stream = fontFile.Open(FileAccess.Read))
                return FontFamily(stream);
        }
#endif

        public static string FontFamily(Stream fontFileStream)
        {
            try
            {
                var analyzer = new TTFAnalyzer();

                // Parses the TTF file format.
                // See http://developer.apple.com/fonts/ttrefman/rm06/Chap6.html
                //_stream = new RandomAccessFile(fontFilename, "r");
                analyzer._stream = fontFileStream;

                // Read the version first
                int version = analyzer.ReadDword();

                // The version must be either 'true' (0x74727565) or 0x00010000 or 'OTTO' (0x4f54544f) for CFF style fonts.
                if (version != 0x74727565 && version != 0x00010000 && version != 0x4f54544f)
                    return null;

                // The TTF file consist of several sections called "tables", and we need to know how many of them are there.
                int numTables = analyzer.ReadWord();

                // Skip the rest in the header
                analyzer.ReadWord(); // skip searchRange
                analyzer.ReadWord(); // skip entrySelector
                analyzer.ReadWord(); // skip rangeShift

                // Now we can read the tables
                for (int i = 0; i < numTables; i++)
                {
                    // Read the table entry
                    int tag = analyzer.ReadDword();
                    analyzer.ReadDword(); // skip checksum
                    int offset = analyzer.ReadDword();
                    int length = analyzer.ReadDword();

                    // Now here' the trick. 'name' field actually contains the textual string name.
                    // So the 'name' string in characters equals to 0x6E616D65
                    if (tag == 0x6E616D65)
                    {
                        // Here's the name section. Read it completely into the allocated buffer
                        var table = new byte[length];

                        analyzer.Seek(offset);
                        analyzer.Read(table);

                        // This is also a table. See http://developer.apple.com/fonts/ttrefman/rm06/Chap6name.html
                        // According to Table 36, the total number of table records is stored in the second word, at the offset 2.
                        // Getting the count and string offset - remembering it's big endian.
                        int count = analyzer.GetWord(table, 2);
                        int string_offset = analyzer.GetWord(table, 4);

                        //List<string> names = new List<string>();

                        // Record starts from offset 6
                        for (int record = 0; record < count; record++)
                        {
                            // Table 37 tells us that each record is 6 words -> 12 bytes, and that the nameID is 4th word so its offset is 6.
                            // We also need to account for the first 6 bytes of the header above (Table 36), so...
                            int nameid_offset = record * 12 + 6;
                            int platformID = analyzer.GetWord(table, nameid_offset);
                            int nameid_value = analyzer.GetWord(table, nameid_offset + 6);


                            // Table 42 lists the valid name Identifiers. We're interested in 1 (Font Family Name) but not in Unicode encoding (for simplicity).
                            // The encoding is stored as PlatformID and we're interested in Mac encoding
                            if (nameid_value == 1) // && platformID == 1)
                            {
                                // We need the string offset and length, which are the word 6 and 5 respectively
                                int name_length = analyzer.GetWord(table, nameid_offset + 8);
                                int name_offset = analyzer.GetWord(table, nameid_offset + 10);

                                // The real name string offset is calculated by adding the string_offset
                                name_offset = name_offset + string_offset;

                                // Make sure it is inside the array
                                if (name_offset >= 0 && name_offset + name_length < table.Length)
                                {
                                    //return new String( table, name_offset, name_length );
                                    //char[] chars = new char[name_length];
                                    /*
									System.Buffer.BlockCopy(table, name_offset, chars, 0, name_length);
									*/
                                    /*
									for(int nameI=0;nameI<name_length;nameI++) {
										chars [nameI] = (char)table [name_offset + nameI];
									}
									*/
                                    byte[] chars = new byte[name_length];
                                    System.Buffer.BlockCopy(table, name_offset, chars, 0, name_length);
                                    //var str = new string(chars);
                                    //var str = System.Text.Encoding.Default.GetString(chars);
                                    if (platformID == 1)
                                    { 
                                        var str = System.Text.Encoding.UTF8.GetString(chars, 0, name_length);
                                        return str;
                                    }
                                    else if (platformID == 2)
                                    {

                                    }
                                    else if (platformID == 3)
                                    {
                                        var str = System.Text.Encoding.BigEndianUnicode.GetString(chars, 0, name_length);
                                        return str;
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
#pragma warning disable 0168
            catch (FileNotFoundException e)
#pragma warning restore 0168
            {
                // Permissions?
                return null;
            }
#pragma warning disable 0168
            catch (IOException e)
#pragma warning restore 0168
            {
                // Most likely a corrupted font file
                return null;
            }
        }


#if NETSTANDARD
        public static string FontAttributes(string fontFilePath)
        {
            using (var stream = System.IO.File.Open(fontFilePath, FileMode.Open))
                return FontAttributes(stream);
        }
#else
        public static string FontAttributes(IFile fontFile)
        {
            using(var stream = fontFile.Open(FileAccess.Read))
            return FontAttributes(stream);
        }
#endif

        public static string FontAttributes(Stream stream)
        {
            try
            {
                var analyzer = new TTFAnalyzer();

                // Parses the TTF file format.
                // See http://developer.apple.com/fonts/ttrefman/rm06/Chap6.html
                //_stream = new RandomAccessFile(fontFilename, "r");
                analyzer._stream = stream;

                // Read the version first
                int version = analyzer.ReadDword();

                // The version must be either 'true' (0x74727565) or 0x00010000 or 'OTTO' (0x4f54544f) for CFF style fonts.
                if (version != 0x74727565 && version != 0x00010000 && version != 0x4f54544f)
                    return null;

                // The TTF file consist of several sections called "tables", and we need to know how many of them are there.
                int numTables = analyzer.ReadWord();

                // Skip the rest in the header
                analyzer.ReadWord(); // skip searchRange
                analyzer.ReadWord(); // skip entrySelector
                analyzer.ReadWord(); // skip rangeShift

                // Now we can read the tables
                for (int i = 0; i < numTables; i++)
                {
                    // Read the table entry
                    int tag = analyzer.ReadDword();
                    analyzer.ReadDword(); // skip checksum
                    int offset = analyzer.ReadDword();
                    int length = analyzer.ReadDword();

                    // Now here' the trick. 'name' field actually contains the textual string name.
                    // So the 'name' string in characters equals to 0x6E616D65
                    if (tag == 0x6E616D65)
                    {
                        // Here's the name section. Read it completely into the allocated buffer
                        var table = new byte[length];

                        //_stream.Seek(offset);
                        analyzer.Seek(offset);
                        analyzer.Read(table);

                        // This is also a table. See http://developer.apple.com/fonts/ttrefman/rm06/Chap6name.html
                        // According to Table 36, the total number of table records is stored in the second word, at the offset 2.
                        // Getting the count and string offset - remembering it's big endian.
                        int count = analyzer.GetWord(table, 2);
                        int string_offset = analyzer.GetWord(table, 4);

                        // Record starts from offset 6
                        for (int record = 0; record < count; record++)
                        {
                            // Table 37 tells us that each record is 6 words -> 12 bytes, and that the nameID is 4th word so its offset is 6.
                            // We also need to account for the first 6 bytes of the header above (Table 36), so...
                            int nameid_offset = record * 12 + 6;
                            int platformID = analyzer.GetWord(table, nameid_offset);
                            int nameid_value = analyzer.GetWord(table, nameid_offset + 6);

                            // Table 42 lists the valid name Identifiers. We're interested in 1 (Font Subfamily Name) but not in Unicode encoding (for simplicity).
                            // The encoding is stored as PlatformID and we're interested in Mac encoding
                            if (nameid_value == 2 && platformID == 1)
                            {
                                // We need the string offset and length, which are the word 6 and 5 respectively
                                int name_length = analyzer.GetWord(table, nameid_offset + 8);
                                int name_offset = analyzer.GetWord(table, nameid_offset + 10);

                                // The real name string offset is calculated by adding the string_offset
                                name_offset = name_offset + string_offset;

                                // Make sure it is inside the array
                                if (name_offset >= 0 && name_offset + name_length < table.Length)
                                {
                                    //return new String( table, name_offset, name_length );
                                    //char[] chars = new char[name_length];
                                    /*
									System.Buffer.BlockCopy(table, name_offset, chars, 0, name_length);
									*/
                                    /*
									for(int nameI=0;nameI<name_length;nameI++) {
										chars [nameI] = (char)table [name_offset + nameI];
									}
									*/
                                    byte[] chars = new byte[name_length];
                                    System.Buffer.BlockCopy(table, name_offset, chars, 0, name_length);
                                    //var str = new string(chars);
                                    //var str = System.Text.Encoding.Default.GetString(chars);
                                    var str = System.Text.Encoding.UTF8.GetString(chars, 0, name_length);
                                    return str;
                                }
                            }
                        }
                    }
                }

                return null;
            }
#pragma warning disable 0168
            catch (FileNotFoundException e)
#pragma warning restore 0168
            {
                // Permissions?
                return null;
            }
#pragma warning disable 0168
            catch (IOException e)
#pragma warning restore 0168
            {
                // Most likely a corrupted font file
                return null;
            }
        }
    }
}

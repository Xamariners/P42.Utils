﻿using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Numerics;

namespace P42.Utils
{
    public static class JsonExtensions
    {
        public static bool IsSimple(this Type type)
        {
            var info = type.GetTypeInfo();
            return info.IsPrimitive || info.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal));
        }

        public static List<T> ReadIJsonReadableList<T>(this JsonReader reader) where T : IJsonReadable<T>, new()
        {
            var result = new List<T>();
            T value;
            reader.Read();
            if (reader.TokenType != JsonToken.StartArray)
                throw new InvalidDataContractException("For the time being, all lists are assumed to be within square brackets");

            reader.Read();
            while (reader.TokenType != JsonToken.EndArray)
            {
                //value = Activator.CreateInstance<T>();
                value = new T();
                value.PropertiesFrom(reader);
                result.Add(value);
                reader.Read();
            }
            return result;
        }

        public static List<T> ReadSimpleList<T>(this JsonReader reader)
        {
            var result = new List<T>();
            T value;
            reader.Read();
            if (reader.TokenType != JsonToken.StartArray)
                throw new InvalidDataContractException("For the time being, all lists are assumed to be within square brackets");

            reader.Read();
            while (reader.TokenType != JsonToken.EndArray)
            {
                value = reader.ParseSimple<T>();
                result.Add(value);
                reader.Read();
            }
            return result;
        }

        /*
		public static List<T> ReadJsonList<T>(this JsonReader reader)
		{
			bool isIJsonReadable = typeof(T).IsIJsonReadable();
			if (!typeof(T).IsSimple())
				throw new InvalidDataContractException("ParseListProperty only works with IJsonReadable or IsSimple types");
			bool isInt = typeof(T) == typeof(int);
			bool isEnum = typeof(T).GetTypeInfo().IsEnum;
			var result = new List<T>();
			T value;
			reader.Read();
			if (reader.TokenType != JsonToken.StartArray)
				throw new InvalidDataContractException("For the time being, all lists are assumed to be within square brackets");
			reader.Read();
			while (reader.TokenType != JsonToken.EndArray)
			{
				if (isIJsonReadable)
				{
					value = Activator.CreateInstance<T>();
					((IJsonReadable)value).ValueFrom(reader);
				}
				else if (isInt)
					value = (T)(object)Convert.ToInt32(reader.Value);
				else if (isEnum)
					value = reader.ReadEnum<T>();
				else
					value = (T)reader.Value;
				result.Add(value);
				reader.Read();
			}
			return result;
		}
		*/

        public static string ReadString(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseString();
        }

        public static string ParseString(this JsonReader reader)
        {
            return (string)reader.Value;
        }

        public static int ReadInt(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseInt();
        }

        public static int ParseInt(this JsonReader reader)
        {
            return (int)((long)reader.Value);
        }

        public static bool ReadBool(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseBool();
        }

        public static bool ParseBool(this JsonReader reader)
        {
            return (bool)reader.Value;
        }

        public static double ReadDouble(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseDouble();
        }

        public static double ParseDouble(this JsonReader reader)
        {
            double result;
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    var boolVal = (bool)reader.Value;
                    result = boolVal ? 1 : 0;
                    break;
                case JsonToken.Bytes:
                    var bytesVal = (byte[])reader.Value;
                    result = BitConverter.ToDouble(bytesVal, 0);
                    break;
                case JsonToken.Float:
                    var doubleVal = (double)reader.Value;
                    result = doubleVal;
                    break;
                case JsonToken.Integer:
                    var intVal = (double)((long)reader.Value);
                    result = intVal;
                    break;
                case JsonToken.String:
                    var stringVal = (string)reader.Value;
                    if (!double.TryParse(stringVal, out doubleVal))
                        throw new InvalidDataContractException("Could not parse what JSON thinks is a double.");
                    result = doubleVal;
                    break;
                default:
                    throw new InvalidDataContractException("Found JSON invalid value token [" + reader.Value + "] of type [" + reader.TokenType + "]");
            }
            return result;
        }

        public static DateTime ReadDateTime(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseDateTime();
        }

        public static DateTime ParseDateTime(this JsonReader reader)
        {
            /*
			var nullDateTime = reader.ReadAsDateTime();
			if (nullDateTime == null)
				throw new InvalidDataContractException("Unable to reader DateTime from [" + reader.Value + "]");
			return nullDateTime.Value;
			*/
            if (reader.TokenType != JsonToken.Date)
                throw new InvalidDataContractException("Expecting JsonToken.Date but found [" + reader.TokenType + "]");
            return (DateTime)reader.Value;
        }

        public static T ReadSimple<T>(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseSimple<T>();
        }

        public static T ParseSimple<T>(this JsonReader reader)
        {
            T result;
            var typeT = typeof(T);
            if (typeT == typeof(bool))
            {
                result = (T)((object)((bool)reader.Value));
            }
            else if (typeT.GetTypeInfo().IsEnum)
            {
                return reader.ParseEnum<T>();
            }
            else if (typeT == typeof(string))
            {
                result = (T)reader.Value;
            }
            else if (typeT == typeof(int))
            {
                var obj = (object)Convert.ToInt32(reader.Value);
                result = (T)obj;
            }
            else if (typeT == typeof(double))
            {
                var val = reader.ParseDouble();
                result = (T)((object)val);
            }
            else if (typeT == typeof(DateTime))
            {
                var val = reader.ParseDateTime();
                result = (T)((object)val);
            }
            else
                throw new InvalidDataContractException("Type [" + typeT + "] is not supported by ReadJsonSimple [" + reader.ValueType + "] [" + reader.Value + "]");
            return result;
        }



        public static T ReadEnum<T>(this JsonReader reader)
        {
            reader.Read();
            return reader.ParseEnum<T>();
        }

        public static T ParseEnum<T>(this JsonReader reader)
        {
            T result;
            if (reader.TokenType == JsonToken.Integer)
                result = (T)Enum.ToObject(typeof(T), reader.Value);
            else
            {
                //var valueStr = reader.ParseStringProperty();
                result = (T)Enum.Parse(typeof(T), (string)reader.Value);
            }
            return result;
        }

        public static T ReadIJsonReadable<T>(this JsonReader reader) where T : IJsonReadable<T>, new()
        {
            reader.Read();
            return reader.ParseIJsonReadable<T>();
        }

        public static T ParseIJsonReadable<T>(this JsonReader reader) where T : IJsonReadable<T>, new()
        {
            var result = new T();
            result.PropertiesFrom(reader);
            return result;
        }

        public static void WriteSafePropertyName(this JsonWriter writer, string name)
        {
            writer.WritePropertyName(name.ReplaceIllegalCharacters());
        }

        public static void WriteList<T>(this JsonWriter writer, IEnumerable<T> list)
        {
            writer.WriteStartArray();
            foreach (T item in list)
                if (typeof(IJsonWriteable).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
                    //writer.WriteValue((IJsonWriteable)item);
                    ((IJsonWriteable)item).WriteValue(writer);
                else
                    writer.WriteValue(item);
            writer.WriteEnd();
            //writer.WriteEndArray();
        }


        /*
        public static void WritePvPair(this JsonWriter writer, string name, bool value)
        {
            if (value)
            {
                writer.WriteSafePropertyName(name);
                writer.WriteValue(value);
            }
        }

        public static void WritePvPair(this JsonWriter writer, string name, int value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, double value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteSafePropertyName(name);
                writer.WriteValue(value);
            }
        }

        public static void WritePvPair(this JsonWriter writer, string name, DateTime value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }
        */



        public static void WritePvPair(this JsonWriter writer, string name, BigInteger value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, BigInteger? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, bool value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, bool? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, byte value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, byte? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, byte[] value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, char value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, char? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, DateTime value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, DateTime? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, DateTimeOffset value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, DateTimeOffset? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, decimal value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, decimal? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, double value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, double? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, float value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, float? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, Guid value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, Guid? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, int value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, int? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, long value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, long? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, sbyte value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, sbyte? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, short value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, short? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, string value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, TimeSpan value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, TimeSpan? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, ushort value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, ushort? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, uint value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, uint? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, ulong value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, ulong? value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }

        public static void WritePvPair(this JsonWriter writer, string name, Uri value)
        {
            writer.WriteSafePropertyName(name);
            writer.WriteValue(value);
        }


        public static void WritePvPair(this JsonWriter writer, string name, IJsonWriteable value)
        {
            if (value != null)
            {
                writer.WriteSafePropertyName(name);
                value.WriteValue(writer);
            }
        }

        public static void WritePvPair<T>(this JsonWriter writer, string name, IEnumerable<T> enumerable)
        {
            if (enumerable != null)
            {
                var collection = enumerable as ICollection<T>;
                if (collection != null && collection.Count == 0)
                    return;
                writer.WriteSafePropertyName(name);
                writer.WriteList(enumerable);
            }
        }
    }
}

﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Amqp.Encoding
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    sealed class SymbolEncoding : PrimitiveEncoding
    {
        public SymbolEncoding()
            : base(FormatCode.Symbol32)
        {
        }

        public static int GetValueSize(AmqpSymbol value)
        {
            return value.Value == null ? FixedWidth.Null : Encoding.ASCII.GetByteCount(value.Value);
        }

        public static int GetEncodeSize(AmqpSymbol value)
        {
            if (value.Value == null)
            {
                return FixedWidth.NullEncoded;
            }

            int valueSize = GetValueSize(value);
            return FixedWidth.FormatCode + AmqpEncoding.GetEncodeWidthBySize(valueSize) + valueSize;
        }

        public static void Encode(AmqpSymbol value, ByteBuffer buffer)
        {
            if (value.Value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                byte[] encodedData = Encoding.ASCII.GetBytes(value.Value);
                int encodeWidth = AmqpEncoding.GetEncodeWidthBySize(encodedData.Length);
                AmqpBitConverter.WriteUByte(buffer, encodeWidth == FixedWidth.UByte ? FormatCode.Symbol8 : FormatCode.Symbol32);
                SymbolEncoding.Encode(encodedData, encodeWidth, buffer);
            }
        }

        public static AmqpSymbol Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return new AmqpSymbol();
            }

            int count;
            AmqpEncoding.ReadCount(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, out count);
            string value = Encoding.ASCII.GetString(buffer.Buffer, buffer.Offset, count);
            buffer.Complete(count);

            return new AmqpSymbol(value);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt + Encoding.ASCII.GetByteCount(((AmqpSymbol)value).Value);
            }
            else
            {
                return SymbolEncoding.GetEncodeSize((AmqpSymbol)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                SymbolEncoding.Encode(Encoding.ASCII.GetBytes(((AmqpSymbol)value).Value), FixedWidth.UInt, buffer);
            }
            else
            {
                SymbolEncoding.Encode((AmqpSymbol)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return SymbolEncoding.Decode(buffer, formatCode);
        }

        static void Encode(byte[] encodedData, int width, ByteBuffer buffer)
        {
            Encode(encodedData, encodedData.Length, width, buffer);
        }

        static void Encode(byte[] encodedData, int encodedDataLength, int width, ByteBuffer buffer)
        {
            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)encodedDataLength);
            }
            else
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)encodedDataLength);
            }

            AmqpBitConverter.WriteBytes(buffer, encodedData, 0, encodedDataLength);
        }

        public override int GetArrayEncodeSize(IList value)
        {
            IReadOnlyList<AmqpSymbol> listValue = (IReadOnlyList<AmqpSymbol>)value;

            int size = 0;
            foreach (AmqpSymbol item in listValue)
            {
                size += Encoding.ASCII.GetByteCount(item.Value);
            }

            return size;
        }

        public override void EncodeArray(IList value, ByteBuffer buffer)
        {
            IReadOnlyList<AmqpSymbol> listValue = (IReadOnlyList<AmqpSymbol>)value;

            byte[] tempBuffer = null;
            foreach (AmqpSymbol item in listValue)
            {
                string s = item.Value;
                int byteCount = Encoding.ASCII.GetByteCount(s);
                if (tempBuffer == null || tempBuffer.Length < byteCount)
                {
                    tempBuffer = new byte[byteCount];
                }

                int encodedByteCount = Encoding.ASCII.GetBytes(s, 0, s.Length, tempBuffer, 0);

                Encode(tempBuffer, encodedByteCount, FixedWidth.UInt, buffer);
            }
        }

        public override Array DecodeArray(ByteBuffer buffer, int count, FormatCode formatCode)
        {
            AmqpSymbol[] symbolArray = new AmqpSymbol[count];
            for (int i = 0; i < count; ++i)
            {
                symbolArray[i] = SymbolEncoding.Decode(buffer, formatCode);
            }
            return symbolArray;
        }
    }
}
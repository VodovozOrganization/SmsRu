﻿using System;

namespace SmsRu.Helpers
{
    /// <summary>
    /// Вспомогательный класс для конвертации различных данных.
    /// </summary>
    public static class ConvertersHelper
    {
        // http://stackoverflow.com/a/624379
        // Почему этот метод быстрее BitConverter.
        public static string ByteArrayToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = bytes[i] >> 4;
                c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
            }
            return new string(c);
        }
    }
}

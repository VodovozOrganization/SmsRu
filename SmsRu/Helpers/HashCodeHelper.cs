﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace SmsRu.Helpers
{
    /// <summary>
    /// Вспомогательный класс для вычисления хеш-функций.
    /// </summary>
    public static class HashCodeHelper
    {
        /// <summary>
        /// Универсальный метод для вычисления хеш-строки, в зависимости от алгоритма.
        /// </summary>
        /// <param name="input">Входная строка.</param>
        /// <param name="algorithm">Алгоритм вычисления хеш-функции.</param>
        /// <returns>SHA512 хеш.</returns>
        public static string ComputeHash(string input, HashAlgorithm algorithm)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            byte[] hashedBytes = algorithm.ComputeHash(inputBytes);

            return ConvertersHelper.ByteArrayToHex(hashedBytes);
        }        

        /// <summary>
        /// Преобразует входную строку в строку hex.
        /// </summary>
        /// <param name="inputString">Входная строка.</param>
        /// <returns>SHA512 хеш в нижнем регистре (hex).</returns>
        public static string GetSHA512Hash(string inputString)
        {
            using (SHA512 sha = new SHA512Managed())
            {
                byte[] input = Encoding.UTF8.GetBytes(inputString);
                return ConvertersHelper.ByteArrayToHex(sha.ComputeHash(input));
            }
        }
    }
}
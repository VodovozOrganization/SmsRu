using NLog;
using SmsRu.Enumerations;
using SmsRu.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace SmsRu
{
    /// <summary>
    /// Класс для работы с SMS.RU API. ISmsProvider - интерфейс, в котором описаны сигнатуры методов для работы с API.
    /// </summary>
    public class SmsRuProvider : ISmsProvider
    {
        /*
         * Проект открытый, можно использовать как угодно. Сохраняйте только авторство.
         * Официальная документация по API - http://sms.ru/?panel=api&subpanel=method&show=sms/send.
         * Разработчик - gennadykarasev@gmail.com. В случае, если что-то не работает, то писать на эту почту.
         * 
         * Для работы с методами класса, нужно указать в app.config значения для переменных, которые используются в коде ниже.
         * Следите за балансом. Если баланса не хватит, чтобы отправить на все номера - сообщение будет уничтожено (его не получит никто).
         *
         */

        // Адреса-константы для работы с API
        const string tokenUrl = "http://sms.ru/auth/get_token";
        const string sendUrl = "http://sms.ru/sms/send";
        const string statusUrl = "http://sms.ru/sms/status";
        const string costUrl = "http://sms.ru/sms/cost";
        const string balanceUrl = "http://sms.ru/my/balance";
        const string limitUrl = "http://sms.ru/my/limit";
        const string sendersUrl = "http://sms.ru/my/senders";
        const string authUrl = "http://sms.ru/auth/check";
        const string stoplistAddUrl = "http://sms.ru/stoplist/add";
        const string stoplistDelUrl = "http://sms.ru/stoplist/del";
        const string stoplistGetUrl = "http://sms.ru/stoplist/get";

        private readonly SmsRuConfiguration configuration;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SmsRuProvider(SmsRuConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region Отправка сообщений

        public string Send(string from, string to, string text)
        {
            return Send(from, new string[] { to }, text, DateTime.MinValue, EnumAuthenticationTypes.Strong);
        }

        public string Send(string from, string to, string text, DateTime dateTime)
        {
            return Send(from, new string[] { to }, text, dateTime, EnumAuthenticationTypes.Strong);
        }

        public string Send(string from, string to, string text, EnumAuthenticationTypes authType)
        {
            return Send(from, new string[] { to }, text, DateTime.MinValue, authType);
        }

        public string Send(string from, string to, string text, DateTime dateTime, EnumAuthenticationTypes authType)
        {
            return Send(from, new string[] { to }, text, dateTime, authType);
        }

        public string Send(string from, string[] to, string text)
        {
            return Send(from, to, text, DateTime.MinValue, EnumAuthenticationTypes.Strong);
        }

        public string Send(string from, string[] to, string text, DateTime dateTime)
        {
            return Send(from, to, text, dateTime, EnumAuthenticationTypes.Strong);
        }

        public string Send(string from, string[] to, string text, EnumAuthenticationTypes authType)
        {
            return Send(from, to, text, DateTime.MinValue, authType);
        }
        
        public string Send(string from, string[] to, string text, DateTime dateTime, EnumAuthenticationTypes authType)
        {
            // TODO: Нужно проверить хватит ли баланса. Баланса не хватит, чтобы отправить на все номера - сообщение будет уничтожено (его не получит никто).
            string result = string.Empty;

            if (to.Length < 1)
                throw new ArgumentNullException("to", "Неверные входные данные - массив пуст.");
            if (to.Length > 100)
                throw new ArgumentOutOfRangeException("to", "Неверные входные данные - слишком много элементов (больше 100) в массиве.");
            if (dateTime == DateTime.MinValue)
                dateTime = DateTime.Now;
            // Лишнее, не надо генерировать это исключение. Если время меньше текущего времени, сообщение отправляется моментально - правило на сервере.
            // if ((DateTime.Now - dateTime).Days > new TimeSpan(7, 0, 0, 0).Days)
            //    throw new ArgumentOutOfRangeException("dateTime", "Неверные входные данные - должно быть не больше 7 дней с момента подачи запроса.");

            string auth = string.Empty;
            string parameters = string.Empty;
            string answer = string.Empty;
            string recipients = string.Empty;
            string token = string.Empty;

            foreach (string item in to)
            {
                recipients += item + ",";
            }
            recipients = recipients.Substring(0, recipients.Length - 1);

            logger.Log(LogLevel.Info, string.Format("{0}={1}Отправка СМС получателям: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), recipients));

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("api_id={0}", configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512wapi);

                parameters = string.Format("{0}&to={1}&text={2}&from={3}", auth, recipients, text, from);
                if (dateTime != DateTime.MinValue)
                    parameters += "&time=" + TimeHelper.GetUnixTime(dateTime);
                if (configuration.PartnerId != string.Empty)
                    parameters += "&partner_id=" + configuration.PartnerId;
                if (configuration.Translit == true)
                    parameters += "&translit=1";
                if (configuration.Test == true)
                    parameters += "&test=1";


                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", parameters));

                WebRequest request = WebRequest.Create(sendUrl);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                byte[] bytes = Encoding.UTF8.GetBytes(parameters);
                request.ContentLength = bytes.Length;
                Stream os = request.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
                os.Close();

                using (WebResponse resp = request.GetResponse())
                {
                    if (resp == null) return null;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        answer = sr.ReadToEnd().Trim();
                    }
                }

                // http://sms.ru/?panel=api&subpanel=method&show=sms/send

                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnSendRequest.MessageAccepted))
                {
                    result = answer;
                }
                else
                {
                    logger.Log(LogLevel.Info, string.Format("{0}={1}Отправка СМС получателям: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), recipients));

                    // http://sms.ru/?panel=api&subpanel=method&show=sms/send

                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                    result = string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }

        public string SendMultiple(string from, Dictionary<string, string> toAndText)
        {
            return SendMultiple(from, toAndText, DateTime.Now, EnumAuthenticationTypes.Strong);
        }

        public string SendMultiple(string from, Dictionary<string, string> toAndText, DateTime dateTime)
        {
            return SendMultiple(from, toAndText, dateTime, EnumAuthenticationTypes.Strong);
        }

        public string SendMultiple(string from, Dictionary<string, string> toAndText, EnumAuthenticationTypes authType)
        {
            return SendMultiple(from, toAndText, DateTime.Now, authType);
        }

        public string SendMultiple(string from, Dictionary<string, string> toAndText, DateTime dateTime, EnumAuthenticationTypes authType)
        {
            // TODO: Нужно проверить хватит ли баланса. Баланса не хватит, чтобы отправить на все номера - сообщение будет уничтожено (его не получит никто).
            string result = string.Empty;

            if (toAndText.Count < 1)
                throw new ArgumentNullException("to", "Неверные входные данные - массив пуст.");
            if (toAndText.Count > 100)
                throw new ArgumentOutOfRangeException("to", "Неверные входные данные - слишком много элементов (больше 100) в массиве.");
            if (dateTime == DateTime.MinValue)
                dateTime = DateTime.Now;

            // Лишнее, не надо генерировать это исключение. Если время меньше текущего времени, сообщение отправляется моментально - правило на сервере.
            // if ((DateTime.Now - dateTime).Days > new TimeSpan(7, 0, 0, 0).Days)
            //    throw new ArgumentOutOfRangeException("dateTime", "Неверные входные данные - должно быть не больше 7 дней с момента подачи запроса.");

            string auth = string.Empty;
            string parameters = string.Empty;
            string answer = string.Empty;
            string recipients = string.Empty;
            string token = string.Empty;

            foreach (KeyValuePair<string, string> kvp in toAndText)
            {
                recipients += "&multi[" + kvp.Key + "]=" + kvp.Value;
            }

            logger.Log(LogLevel.Info, string.Format("{0}={1}Отправка СМС получателям: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), recipients));

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("api_id={0}", configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512wapi);

                parameters = string.Format("{0}&from={1}{2}", auth, from, recipients);
                if (dateTime != DateTime.MinValue)
                    parameters += "&time=" + TimeHelper.GetUnixTime(dateTime);
                if (configuration.PartnerId != string.Empty)
                    parameters += "&partner_id=" + configuration.PartnerId;
                if (configuration.Translit == true)
                    parameters += "&translit=1";
                if (configuration.Test == true)
                    parameters += "&test=1";

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", parameters));

                WebRequest request = WebRequest.Create(sendUrl);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                byte[] bytes = Encoding.UTF8.GetBytes(parameters);
                request.ContentLength = bytes.Length;
                Stream os = request.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
                os.Close();

                using (WebResponse resp = request.GetResponse())
                {
                    if (resp == null) return null;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        answer = sr.ReadToEnd().Trim();
                    }
                }

                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnSendRequest.MessageAccepted))
                {
                    result = answer;
                }
                else
                {
                    logger.Log(LogLevel.Info, string.Format("{0}={1}Отправка СМС получателям: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), recipients));

                    result = string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }
            return result;
        }
        
        public ResponseOnSendRequest SendByEmail(string[] to, string text)
        {
            /*
             * Используется отправка по SMTP протоколу.
             * Надежность заключается в том, что в случае если между вашим и нашим сервером наблюдается ошибка связи, протокол SMTP обеспечит гарантированную повторную отправку вашего сообщения.
             * Если бы вы использовали стандартный метод sms/send, вам бы пришлось отслеживать эти ошибки и дополнительно разрабатывать дополнительный программный код для обработки очереди исходящих сообщений.
             */

            ResponseOnSendRequest result = ResponseOnSendRequest.Error;

            if (to.Length < 1)
                throw new ArgumentNullException("to", "Неверные входные данные - массив пуст.");
            if (to.Length > 50)
                throw new ArgumentOutOfRangeException("to", "Неверные входные данные - слишком много элементов (больше 50) в массиве.");

            // TODO: Нужно проверить хватит ли баланса. Баланса не хватит, чтобы отправить на все номера - сообщение будет уничтожено (его не получит никто).

            string recipients = string.Empty;

            try
            {
                foreach (string item in to)
                {
                    recipients += item + ",";
                }
                recipients = recipients.Substring(0, recipients.Length - 1);

                logger.Log(LogLevel.Info, string.Format("{0}={1}Отправка СМС получателям: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), recipients));

                var smtp = new SmtpClient
                {
                    Host = configuration.SmtpServer,
                    Port = configuration.SmtpPort,
                    EnableSsl = configuration.SmtpUseSSL,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(configuration.SmtpLogin, configuration.SmtpPassword),
                    Timeout = 20000
                };
                using (var message = new MailMessage(configuration.Email, configuration.EmailToSmsGateEmail)
                {
                    Subject = recipients,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = false,
                    Body = text
                })
                {
                    smtp.Send(message);

                    logger.Log(LogLevel.Info, string.Format("Текст: {0} Письмо успешно отправлено.", text));
                }

                result = ResponseOnSendRequest.MessageAccepted;

            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);

                result = ResponseOnSendRequest.Error;
            }
            return result;
        }

        #endregion

        #region Проверка статуса сообщения
        
        public ResponseOnStatusRequest CheckStatus(string id, EnumAuthenticationTypes authType)
        {
            ResponseOnStatusRequest result = ResponseOnStatusRequest.MethodNotFound;

            logger.Log(LogLevel.Info, string.Format("{0}={1}Проверка статуса по сообщению: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), id));

            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", statusUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", statusUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", statusUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}&id={1}", auth, id);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnStatusRequest.MessageRecieved))
                                {
                                    result = ResponseOnStatusRequest.MessageRecieved;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1}Проверка статуса по сообщению: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), id));
                                    logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                    result = (ResponseOnStatusRequest)Convert.ToInt32(lines[0]);
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);

                result = ResponseOnStatusRequest.MessageNotFoundOrError;
            }
            return result;
        }
        #endregion

        #region Узнать стоимость сообщения и количество необходимых для отправки сообщений
        
        public string CheckCost(string to, string text, EnumAuthenticationTypes authType)
        {
            string result = string.Empty;

            logger.Log(LogLevel.Info, string.Format("{0}={1}Cтоимость сообщения и количество необходимых для отправки сообщений: {2} Сообщение: {3}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), to, text));

            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", costUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", costUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", costUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}&to={1}&text={2}", auth, to, text);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnCostRequest.Done))
                                {
                                    result = answer;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1}Cтоимость сообщения и количество необходимых для отправки сообщений: {2} Сообщение: {3}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), to, text));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                    result = string.Empty;
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }
        #endregion

        #region Получение состояния баланса
        
        public string CheckBalance(EnumAuthenticationTypes authType)
        {
            string result = string.Empty;

            logger.Log(LogLevel.Info, string.Format("{0}={1} Получение состояния баланса", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));

            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", balanceUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", balanceUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", balanceUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}", auth);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnBalanceRequest.Done))
                                {
                                    result = answer;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1} Получение состояния баланса", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                    result = string.Empty;
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }
        #endregion

        #region Получение текущего состояния дневного лимита
       
        public string CheckLimit(EnumAuthenticationTypes authType)
        {
            string result = string.Empty;

            logger.Log(LogLevel.Info, string.Format("{0}={1} Получение текущего состояния дневного лимита:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));

            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", limitUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", limitUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", limitUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}", auth);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnLimitRequest.Done))
                                {
                                    result = answer;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1} Получение текущего состояния дневного лимита:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                    result = string.Empty;
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }
            
            return result;
        }
        #endregion

        #region Получение списка отправителей
        
        public string CheckSenders(EnumAuthenticationTypes authType)
        {
            string result = string.Empty;

            logger.Log(LogLevel.Info, string.Format("{0}={1} Получение списка отправителей:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", sendersUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", sendersUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", sendersUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}", auth);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnSendersRequest.Done))
                                {
                                    result = answer;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1} Получение списка отправителей:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }
        #endregion

        #region Получение токена
        
        public string GetToken()
        {
            string result = string.Empty;

            try
            {
                WebRequest request = WebRequest.Create(tokenUrl);
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                result = sr.ReadToEnd();
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла ошибка при получении токена по адресу http://sms.ru/auth/get_token. " + ex.Message);
                logger.Log(LogLevel.Trace, ex.StackTrace);
            }
            return result;
        }
        #endregion

        #region Проверка статуса сообщения
        
        public ResponseOnAuthRequest AuthCheck(EnumAuthenticationTypes authType)
        {
            ResponseOnAuthRequest result = ResponseOnAuthRequest.Error;

            logger.Log(LogLevel.Info, string.Format("{0}={1} Проверка номера телефона и пароля на действительность:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));

            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", authUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", authUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", authUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}", auth);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnAuthRequest.Done))
                                {
                                    result = ResponseOnAuthRequest.Done;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1} Проверка номера телефона и пароля на действительность:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                    result = (ResponseOnAuthRequest)Convert.ToInt32(lines[0]);
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);

                result = ResponseOnAuthRequest.Error;
            }
            
            return result;
        }
        #endregion

        #region Операции с Stoplist
        
        public bool StoplistAdd(string phone, string text, EnumAuthenticationTypes authType)
        {
            bool result = false;

            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text", "Неверные входные данные - обязательный параметр.");


            string auth = string.Empty;
            string parameters = string.Empty;
            string answer = string.Empty;
            string recipients = string.Empty;
            string token = string.Empty;

            logger.Log(LogLevel.Info, string.Format("Добавление номера в стоплист: Номер: {0}, Примечание: {1}", phone, text));

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("api_id={0}", configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512wapi);

                parameters = string.Format("{0}&stoplist_phone={1}&stoplist_text={2}", auth, phone, text);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", parameters));

                WebRequest request = WebRequest.Create(stoplistAddUrl);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                byte[] bytes = Encoding.UTF8.GetBytes(parameters);
                request.ContentLength = bytes.Length;
                Stream os = request.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
                os.Close();

                using (WebResponse resp = request.GetResponse())
                {
                    if (resp == null) return false;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        answer = sr.ReadToEnd().Trim();
                    }
                }

                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnStoplistAddRequest.Done))
                {
                    result = true;
                }
                else
                {
                    logger.Log(LogLevel.Info, string.Format("{0}={1}Добавление номера в стоплист: Номер: {2}, Примечание: {3}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), phone, text));
                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                    result = false;
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }
        
        public bool StoplistDelete(string phone, EnumAuthenticationTypes authType)
        {
            bool result = false;

            string auth = string.Empty;
            string parameters = string.Empty;
            string answer = string.Empty;
            string recipients = string.Empty;
            string token = string.Empty;

            logger.Log(LogLevel.Info, string.Format("Удаление номера из стоплиста: Номер: {0}", phone));

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("api_id={0}", configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("login={0}&token={1}&sha512={2}", configuration.Login, token, sha512wapi);

                parameters = string.Format("{0}&stoplist_phone={1}", auth, phone);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", parameters));

                WebRequest request = WebRequest.Create(stoplistDelUrl);
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                byte[] bytes = Encoding.UTF8.GetBytes(parameters);
                request.ContentLength = bytes.Length;
                Stream os = request.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);
                os.Close();

                using (WebResponse resp = request.GetResponse())
                {
                    if (resp == null) return false;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        answer = sr.ReadToEnd().Trim();
                    }
                }

                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnStoplistDeleteRequest.Done))
                {
                    result = true;
                }
                else
                {
                    logger.Log(LogLevel.Info, string.Format("{0}={1}Удаление номера из стоплиста: Номер: {2}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), phone));
                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                    result = false;
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }
        
        public string StoplistGet(EnumAuthenticationTypes authType)
        {
            string result = string.Empty;

            string auth = string.Empty;
            string link = string.Empty;
            string answer = string.Empty;
            string token = string.Empty;

            logger.Log(LogLevel.Info, string.Format("Получение номеров из стоплиста:"));

            try
            {
                token = GetToken();

                string sha512 = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}", configuration.Password, token)).ToLower();
                string sha512wapi = HashCodeHelper.GetSHA512Hash(string.Format("{0}{1}{2}", configuration.Password, token, configuration.ApiId)).ToLower();

                if (authType == EnumAuthenticationTypes.Simple)
                    auth = string.Format("{0}?api_id={1}", stoplistGetUrl, configuration.ApiId);
                if (authType == EnumAuthenticationTypes.Strong)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", stoplistGetUrl, configuration.Login, token, sha512);
                if (authType == EnumAuthenticationTypes.StrongApi)
                    auth = string.Format("{0}?login={1}&token={2}&sha512={3}", stoplistGetUrl, configuration.Login, token, sha512wapi);

                link = string.Format("{0}", auth);

                logger.Log(LogLevel.Info, string.Format("Запрос: {0}", link));

                WebRequest req = WebRequest.Create(link);
                using (WebResponse response = req.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        if (stream != null)
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                answer = sr.ReadToEnd();

                                logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));

                                string[] lines = answer.Split(new string[] { "\n" }, StringSplitOptions.None);
                                if (Convert.ToInt32(lines[0]) == Convert.ToInt32(ResponseOnStoplistGetRequest.Done))
                                {
                                    result = answer;
                                }
                                else
                                {
                                    logger.Log(LogLevel.Info, string.Format("{0}={1}Получение номеров из стоплиста:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString()));
                                    logger.Log(LogLevel.Info, string.Format("Ответ: {0}", answer));
                                }
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                    " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен. " +
                    ex.Message);

                logger.Log(LogLevel.Trace, ex.StackTrace);
            }

            return result;
        }
        #endregion
    }
}
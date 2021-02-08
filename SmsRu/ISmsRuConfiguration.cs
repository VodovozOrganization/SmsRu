namespace SmsRu
{
    public interface ISmsRuConfiguration
    {
        /// <summary>
        /// Является вашим секретным кодом, который используется во внешних программах
        /// </summary>
        string ApiId { get; }

        /// <summary>
        /// Ваш email адрес для отправки
        /// </summary>
        string Email { get; }

        /// <summary>
        ///  Ваш уникальный адрес (для отправки СМС по email)
        /// </summary>
        string EmailToSmsGateEmail { get; }

        /// <summary>
        /// Логин для доступа к сервису SMS.RU
        /// </summary>
        string Login { get; }

        /// <summary>
        /// Если вы участвуете в партнерской программе, укажите этот параметр в запросе
        /// </summary>
        string PartnerId { get; }

        /// <summary>
        /// Пароль для доступа к сервису SMS.RU
        /// </summary>
        string Password { get; }

        /// <summary>
        /// Логин для авторизации на SMTP-сервере
        /// </summary>
        string SmtpLogin { get; }

        /// <summary>
        /// Пароль для авторизации на SMTP-сервере
        /// </summary>
        string SmtpPassword { get; }

        /// <summary>
        /// Порт для авторизации на SMTP-сервере
        /// </summary>
        int SmtpPort { get; }

        /// <summary>
        /// SMTP-сервер
        /// </summary>
        string SmtpServer { get; }

        /// <summary>
        /// Флаг - использовать SSL при подключении к серверу SMTP
        /// </summary>
        bool SmtpUseSSL { get; }

        /// <summary>
        /// Имитирует отправку сообщения для тестирования ваших программ на правильность обработки ответов сервера. При этом само сообщение не отправляется и баланс не расходуется.
        /// </summary>
        bool Test { get; }

        /// <summary>
        /// Переводит все русские символы в латинские
        /// </summary>
        bool Translit { get; }

        /// <summary>
        /// Номер с которого будет оправлено сообщение (необходимо согласование с администрацией Sms.ru)
        /// </summary>
        string SmsNumberFrom { get; }
    }
}
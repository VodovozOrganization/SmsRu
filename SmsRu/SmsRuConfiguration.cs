using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsRu
{
    public class SmsRuConfiguration
    {
        private readonly string login;
        private readonly string password;
        private readonly string apiId;
        private readonly string partnerId;
        private readonly string emailToSmsGateEmail;
        private readonly string email;
        private readonly string smtpLogin;
        private readonly string smtpPassword;
        private readonly string smtpServer;
        private readonly int smtpPort;
        private readonly bool smtpUsrSSL;
        private readonly bool translit;
        private readonly bool test;

        public SmsRuConfiguration(
            string login,
            string password,
            string apiId,
            string partnerId,
            string email,
            string smtpLogin,
            string smtpPassword,
            string smtpServer,
            int smtpPort,
            bool smtpUsrSSL,
            bool translit,
            bool test
            )
        {
            this.login = login;
            this.password = password;
            this.apiId = apiId;
            this.partnerId = partnerId;
            this.emailToSmsGateEmail = apiId + "@sms.ru";
            this.email = email;
            this.smtpLogin = smtpLogin;
            this.smtpPassword = smtpPassword;
            this.smtpServer = smtpServer;
            this.smtpPort = smtpPort;
            this.smtpUsrSSL = smtpUsrSSL;
            this.translit = translit;
            this.test = test;
        }

        public string Login => login;

        public string Password => password;

        public string ApiId => apiId;

        public string PartnerId => partnerId;

        public string EmailToSmsGateEmail => emailToSmsGateEmail;

        public string Email => email;

        public string SmtpLogin => smtpLogin;

        public string SmtpPassword => smtpPassword;

        public string SmtpServer => smtpServer;

        public int SmtpPort => smtpPort;

        public bool SmtpUseSSL => smtpUsrSSL;

        public bool Translit => translit;

        public bool Test => test;
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace SmsRu.Handlers
{
    public class CallbackHandler : HttpClientHandler
    {
        private readonly ILogger<CallbackHandler> logger;

        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007        /// 
        /// 
        /// Для использования CallbackHandler нужно добавить эти строчки в web.config вашего проекта.
        ///     <handlers>
        ///     <add name="SmsRuCallbackHandler" preCondition="integratedMode" verb="*" type="SmsRu.Handlers.CallbackHandler" path="/SmsRuCallback" />
        ///     </handlers> 
        ///     
        /// http://sms.ru/?panel=apps&subpanel=cb - документация по API
        /// 
        /// </summary>
        #region IHttpHandler Members

        public CallbackHandler(ILogger<CallbackHandler> logger)
        {
            this.logger = logger;
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.Request.Form.Keys.Count > 0)
            {
                try
                {
                    string index = string.Empty;
                    for (int i = 0; i < context.Request.Form.Keys.Count; i++)
                    {
                        index = "data[" + i.ToString() + "]";
                        string[] lines = context.Request.Form[index].ToString().Split(new string[] { "\n" }, StringSplitOptions.None);
                        if (lines[0] == "sms_status")
                        {
                            string smsID = lines[1];
                            string status = lines[2];

                            // http://sms.ru/?panel=apps&subpanel=cb

                            logger.LogInformation("{Date}={Time}Callback:", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());
                            logger.LogInformation("Запрос: {Request}", context.Request.Form[index]);

                            // Ваш код.
                            // Можно использовать EnumResponseCodes для работы со статусами.
                            context.Response.ContentType = "text/plain";
                            context.Response.WriteAsync("100");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Возникла непонятная ошибка. Нужно проверить значения в файле конфигурации и разобраться в коде." +
                        " Скорее всего введены неверные значения, либо сервер SMS.RU недоступен.");
                }
            }
            //context.Response.Flush();
            //context.Response.End();
        }

        #endregion
    }
}

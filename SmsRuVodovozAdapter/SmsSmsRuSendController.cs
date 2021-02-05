using SmsRu;
using SmsSendInterface;
using System;
using System.Threading.Tasks;

namespace SmsRuVodovozAdapter
{
    public class SmsSmsRuSendController : ISmsSender, ISmsBalanceNotifier
    {
        private readonly SmsRuProvider smsRuProvider;

        public SmsSmsRuSendController(ISmsRuConfiguration configuration)
        {

            this.smsRuProvider = new SmsRuProvider(configuration);
        }

        public BalanceResponse GetBalanceResponse
        {
            get
            {
                var balanceResponse = smsRuProvider.CheckBalance(SmsRu.Enumerations.EnumAuthenticationTypes.Simple); // Должно быть из откуда-нибудь

                BalanceResponse balance = new BalanceResponse() { 
                    BalanceType = BalanceType.CurrencyBalance,
                    BalanceValue = decimal.Parse(balanceResponse) 
                };

                return balance;
            }
        }

        public event EventHandler<SmsBalanceEventArgs> OnBalanceChange;

        public ISmsSendResult SendSms(ISmsMessage message)
        {
            try
            {
                var response = smsRuProvider.Send("ТУТ ДОЛЖЕН БЫТЬ НОМЕР С КОТОРОГО ОТПРАВЛЯЕТСЯ СМС", message.MobilePhoneNumber, message.MessageText, message.ScheduleTime); // TODO: это в конфигурацию
                return new SmsSendResult(SmsSentStatus.Accepted);
            } catch (Exception ex)
            {
                throw ex;
            }
        }

        public Task<ISmsSendResult> SendSmsAsync(ISmsMessage message)
        {
            throw new NotSupportedException(); // Нет использований в нашем проекте TODO: дописать при рефакторинге библиотеки
        }
    }
}

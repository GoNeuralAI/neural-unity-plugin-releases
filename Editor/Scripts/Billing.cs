using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Neural
{
    public struct ApiBillingModel
    {
        [JsonProperty("subscription")]
        public string Subscription { get; set; }

        [JsonProperty("remaining")]
        public int Credits { get; set; }
    }

    public class Billing
    {
        public ApiBillingModel Model { get; private set; }

        public delegate void CreditsUpdatedDelegate();

        public event CreditsUpdatedDelegate OnCreditsUpdated;

        private bool isTimerRunning = false;

        public Billing() {
            UpdateBilling().ContinueWith(t =>
            {
                _ = BillingTimer();
            });
        }

        ~Billing()
        {
            isTimerRunning = false;  
        }

        async public Task UpdateBilling(int delay = 0)
        {
            await Task.Delay(delay);

            if (Context.ApiKey == "")
            {
                return;
            }

            var httpClient = new HttpClient();

            Model = await httpClient.MakeApiGetRequest<ApiBillingModel>("billing");

            OnCreditsUpdated?.Invoke();
        }

        private async Task BillingTimer()
        {
            isTimerRunning = true;
            while (isTimerRunning)
            {
                await UpdateBilling(30000);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewSourceAdapter.Models
{
    public static class LocalStoreManager
    {
        private const string StateFileName = "state.json";
        private const string ApproviesFileName = "approvies.json";

        public static void SaveState(ApplicationState applicationState)
        {
            using (FileStream fs = new FileStream(StateFileName, FileMode.Truncate))
            {
                string json = JsonSerializer.Serialize<ApplicationState>(applicationState);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                fs.Write(bytes);
            }
        }

        public static ApplicationState LoadState()
        {
            using (FileStream fs = new FileStream(StateFileName, FileMode.OpenOrCreate))
            {
                return JsonSerializer.DeserializeAsync<ApplicationState>(fs).Result;
            }
        }

        public static void SaveApprovies(ApproviesSaveCard approviesSaveCard)
        {
            using (FileStream fs = new FileStream(ApproviesFileName, FileMode.Truncate))
            {
                string json = JsonSerializer.Serialize<ApproviesSaveCard>(approviesSaveCard);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                fs.Write(bytes);
            }
        }

        public static ApproviesSaveCard LoadApprovies()
        {
            using (FileStream fs = new FileStream(ApproviesFileName, FileMode.OpenOrCreate))
            {
                return JsonSerializer.DeserializeAsync<ApproviesSaveCard>(fs).Result;
            }
        }
    }
}

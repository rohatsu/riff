using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIFF.Core
{
    public static class RFNetworkHelpers
    {
        public static T RetryNetworkOperation<T>(Func<T> operation, int numTries = 3, int delay = 500, bool throwOnFail = true) where T : class
        {
            while (true)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    numTries--;
                    if (numTries > 0)
                    {
                        RFStatic.Log.Warning(typeof(RFNetworkHelpers), "Error trying network operation ({0} retries left): {1}", numTries, ex.Message);
                        if (delay > 0)
                        {
                            System.Threading.Thread.Sleep(delay);
                        }
                    }
                    else if (throwOnFail)
                    {
                        throw;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}

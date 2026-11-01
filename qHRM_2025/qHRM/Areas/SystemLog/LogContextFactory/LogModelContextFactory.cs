using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SystemLog.LogContextFactory
{
    public class LogModelContextFactory
    {
        public ILogModelContext CreateLogContext(qHRMasterContext hrmContext, String modelType)
        {
            switch (modelType)
            {
                case "HRATN":
                    {
                        return new LogHratndcContext(hrmContext);
                    }
                case "HREMP":
                    {
                        return new LogHrempContext(hrmContext);
                    }
                case "HRLV":
                    {
                        return new LogHrlvContext(hrmContext);
                    }
                case "HRPR":
                    {
                        return new LogHrprContext(hrmContext);
                    }
                case "HRWSF":
                    {
                        return new LogHrwsfContext(hrmContext);
                    }
                case "ORG":
                    {
                        return new LogOrgContext(hrmContext);
                    }
                default:  { return null; }
            }
        }
    }
}

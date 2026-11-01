using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notify.Models;
using Microsoft.EntityFrameworkCore;

namespace Notify.Data
{
    public class NotifyContext
    {

        #region ==============Member Variables==============
        private readonly NotificationContext _notifyContext;

        #endregion

        #region ==============Constructors==============
        public NotifyContext (NotificationContext notifyContext)
        {
            _notifyContext = notifyContext;

        }
        #endregion


        public IEnumerable<NtfNotification> GetPushNotifications(int empSysId)
        {
            try
            {
                string sp = "[NTF_Get_PushNotifications] {0}";
                object[] param = { empSysId};
                var list = new List<NtfNotification>();
                list = _notifyContext.NtfNotification.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using qHRM.Areas.Notify.Models;
using Microsoft.EntityFrameworkCore;

namespace qHRM.Areas.Notify.Data
{
    public class NotifyContext
    {

        #region ==============Member Variables==============
        private readonly NotificationContext _notifyContext;

        #endregion

        #region ==============Constructors==============
        public NotifyContext(NotificationContext notifyContext)
        {
            _notifyContext = notifyContext;

        }
        #endregion


        public IEnumerable<NtfNotification> GetPushNotifyList(int empSysId)
        {
            try
            {
                if (empSysId <= 0) return new List<NtfNotification>();
                return _notifyContext.NtfNotification
                    .Where(x => x.EmpSysId == empSysId)
                    .OrderByDescending(x => x.CrtDateTime)
                    .Take(50)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] GetPushNotifyList: {ex.Message}");
                return new List<NtfNotification>();
            }
        }

        public IEnumerable<NtfNotification> GetPushNotifyCount(int empSysId)
        {
            try
            {
                if (empSysId <= 0) return new List<NtfNotification>();
                return _notifyContext.NtfNotification
                    .Where(x => x.EmpSysId == empSysId && (x.PushReadStat == null || x.PushReadStat == false))
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] GetPushNotifyCount: {ex.Message}");
                return new List<NtfNotification>();
            }
        }

        public int ReadPushNotifications(int empSysId)
        {
            try
            {
                if (empSysId <= 0) return 0;
                var unread = _notifyContext.NtfNotification
                    .Where(x => x.EmpSysId == empSysId && (x.PushReadStat == null || x.PushReadStat == false))
                    .ToList();

                foreach (var n in unread)
                {
                    n.PushReadStat = true;
                }
                _notifyContext.SaveChanges();
                return 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFY ERROR] ReadPushNotifications: {ex.Message}");
                return 0;
            }
        }


        public NtfNotification GetSlrySlipEmailNotify(NtfNotification ntf)
        {
            try
            {                
               return _notifyContext.NtfNotification.Where(x => x.EmpSysId == ntf.EmpSysId && x.EftMnth == ntf.EftMnth && x.EftYr == ntf.EftYr).FirstOrDefault();                
                
            }
            catch (Exception )
            {
                return null;
            }


        }


        public int AddSlrySlipEmailNotify(NtfNotification ntf)
        {
            try
            {
                _notifyContext.NtfNotification.Add(ntf);
                _notifyContext.SaveChanges();
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }

            
        }

        public int UpdateSlrySlipEmailNotify(NtfNotification ntf)
        {
            try
            {
                _notifyContext.NtfNotification.Update(ntf);
                _notifyContext.SaveChanges();
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }


        }



    }
}

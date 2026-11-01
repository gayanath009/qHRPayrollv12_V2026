using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SystemLog.LogContextFactory
{
    public interface ILogModelContext
    {
        int insert(int tableId, int recId, int userId);
        int update<T>(int tableId, int recId, T newModel, T existModel, int userId);
        int delete(int tableId, int recId, int userId);
    }
}

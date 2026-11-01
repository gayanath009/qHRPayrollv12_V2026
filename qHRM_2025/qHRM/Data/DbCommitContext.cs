using Microsoft.EntityFrameworkCore;
using qHRM.Models;
using qHRM.Areas.SystemLog.LogContextFactory;
using qHRM.Areas.SystemLog.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Data
{
    public class DbCommitContext<T> where T : class
    {
        private readonly qHRMasterContext _hrmContext;
        private LogModelContextFactory _logModelContextFactory;

        public DbCommitContext(qHRMasterContext hrmContext)
        {
            _hrmContext = hrmContext;
            _logModelContextFactory = new LogModelContextFactory();
        }

        public int commitChanges(LogEventTypes eventType, T model, int userId) {
            int result = 0;
            var modelType = model.GetType();
            var modelName = modelType.Name;

            try
            {
                if (modelType.GetProperty("RecStat") != null && eventType == LogEventTypes.INS)
                    modelType.GetProperty("RecStat").SetValue(model, (Int16)1);

                var tableInfo = getTableInfo(eventType, modelName);

                using (var dbContextTransaction = _hrmContext.Database.BeginTransaction())
                {
                    try
                    {
                        result = 1;
                        switch (eventType)
                        {
                            case LogEventTypes.INS:
                                {
                                    _hrmContext.Set<T>().Add(model);

                                    if (modelType.GetProperty("CrtDateTime") != null)
                                        modelType.GetProperty("CrtDateTime").SetValue(model, DateTime.Now);

                                    if (modelType.GetProperty("CrtId") != null)
                                        modelType.GetProperty("CrtId").SetValue(model, userId);

                                    if (tableInfo != null)
                                    {
                                        _hrmContext.SaveChanges();
                                        // Get Inserted Record Key
                                        int recId = GetId(model);

                                        ILogModelContext logModelContext = _logModelContextFactory.CreateLogContext(_hrmContext, tableInfo.Section);
                                        if (logModelContext != null)
                                            result = logModelContext.insert(tableInfo.TblId, recId, userId);
                                    }
                                    break;
                                }
                            case LogEventTypes.UPD:
                                {
                                    int recId = GetId(model);

                                    var extModel = _hrmContext.Set<T>().Find(recId);
                                    _hrmContext.Entry(extModel).State = EntityState.Detached;

                                    if (modelType.GetProperty("CrtDateTime") != null)
                                        modelType.GetProperty("CrtDateTime").SetValue(model, extModel.GetType().GetProperty("CrtDateTime").GetValue(extModel, null));

                                    if (modelType.GetProperty("CrtId") != null)
                                        modelType.GetProperty("CrtId").SetValue(model, extModel.GetType().GetProperty("CrtId").GetValue(extModel, null));


                                    var varChanges = _hrmContext.Set<T>().Attach(model);
                                    varChanges.State = EntityState.Modified;
                                    if (tableInfo != null)
                                    {
                                        ILogModelContext logModelContext = _logModelContextFactory.CreateLogContext(_hrmContext, tableInfo.Section);
                                        if(logModelContext != null)
                                            result = logModelContext.update<T>(tableInfo.TblId, recId, model, extModel, userId);
                                    }
                                    break;
                                }
                            case LogEventTypes.DEL:
                                {
                                    int recId = GetId(model);

                                    _hrmContext.Set<T>().Remove(model);
                                    if (tableInfo != null)
                                    {
                                        ILogModelContext logModelContext = _logModelContextFactory.CreateLogContext(_hrmContext, tableInfo.Section);
                                        if (logModelContext != null)
                                            result = logModelContext.delete(tableInfo.TblId, recId, userId);
                                    }
                                    break;
                                }
                        }
                        _hrmContext.SaveChanges();
                        dbContextTransaction.Commit();
                    }
                    catch {
                        dbContextTransaction.Rollback();
                        result = 0;
                        throw; 
                    }
                    return result;
                }
            }
            catch (Exception ex) { throw ex; }
        }


        public async Task<int> commitChangesAsync(LogEventTypes eventType, T model, int userId)
        {
            int result = 0;
            var modelType = model.GetType();
            var modelName = modelType.Name;

            try
            {
                if (modelType.GetProperty("RecStat") != null && eventType == LogEventTypes.INS)
                    modelType.GetProperty("RecStat").SetValue(model, (Int16)1);

                var tableInfo = getTableInfo(eventType, modelName);

                using (var dbContextTransaction = _hrmContext.Database.BeginTransaction())
                {
                    try
                    {
                        result = 1;
                        switch (eventType)
                        {
                            case LogEventTypes.INS:
                                {
                                    _hrmContext.Set<T>().Add(model);

                                    if (modelType.GetProperty("CrtDateTime") != null)
                                        modelType.GetProperty("CrtDateTime").SetValue(model, DateTime.Now);

                                    if (modelType.GetProperty("CrtId") != null)
                                        modelType.GetProperty("CrtId").SetValue(model, userId);

                                    if (tableInfo != null)
                                    {
                                        _hrmContext.SaveChanges();
                                        // Get Inserted Record Key
                                        int recId = GetId(model);

                                        ILogModelContext logModelContext = _logModelContextFactory.CreateLogContext(_hrmContext, tableInfo.Section);
                                        if (logModelContext != null)
                                            result = await Task.FromResult(logModelContext.insert(tableInfo.TblId, recId, userId));
                                    }
                                    break;
                                }
                            case LogEventTypes.UPD:
                                {
                                    int recId = GetId(model);

                                    var extModel = _hrmContext.Set<T>().Find(recId);
                                    _hrmContext.Entry(extModel).State = EntityState.Detached;

                                    if (modelType.GetProperty("CrtDateTime") != null)
                                        modelType.GetProperty("CrtDateTime").SetValue(model, extModel.GetType().GetProperty("CrtDateTime").GetValue(extModel, null));

                                    if (modelType.GetProperty("CrtId") != null)
                                        modelType.GetProperty("CrtId").SetValue(model, extModel.GetType().GetProperty("CrtId").GetValue(extModel, null));


                                    var varChanges = _hrmContext.Set<T>().Attach(model);
                                    varChanges.State = EntityState.Modified;
                                    if (tableInfo != null)
                                    {
                                        ILogModelContext logModelContext = _logModelContextFactory.CreateLogContext(_hrmContext, tableInfo.Section);
                                        if (logModelContext != null)
                                            result = await Task.FromResult(logModelContext.update<T>(tableInfo.TblId, recId, model, extModel, userId));
                                    }
                                    break;
                                }
                            case LogEventTypes.DEL:
                                {
                                    int recId = GetId(model);

                                    _hrmContext.Set<T>().Remove(model);
                                    if (tableInfo != null)
                                    {
                                        ILogModelContext logModelContext = _logModelContextFactory.CreateLogContext(_hrmContext, tableInfo.Section);
                                        if (logModelContext != null)
                                            result = await Task.FromResult(logModelContext.delete(tableInfo.TblId, recId, userId));
                                    }
                                    break;
                                }
                        }
                        _hrmContext.SaveChanges();
                        dbContextTransaction.Commit();
                    }
                    catch
                    {
                        dbContextTransaction.Rollback();
                        result = 0;
                        throw;
                    }
                    return result;
                }
            }
            catch (Exception ex) { throw ex; }
        }

        private SysTableInfo getTableInfo(LogEventTypes eventType, String modelName)
        {
            try
            {
                //TODO Should keep in the cache memory
                return _hrmContext.SysTableInfo.Where(x => x.MdlName == modelName && (
                                                       (eventType == LogEventTypes.INS && x.LogInsert == true)
                                                       || (eventType == LogEventTypes.UPD && x.LogUpdate == true)
                                                       || (eventType == LogEventTypes.DEL && x.LogDelete == true)
                                                       )).FirstOrDefault();
            }
            catch { throw; }
        }

        private int GetId(T entity)
        {
            var keyName = _hrmContext.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                .Select(x => x.Name).Single();


            return (int)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        }
    }
}

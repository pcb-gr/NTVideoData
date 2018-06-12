using NTVideoData_v1.Entities;
using System;

namespace NTVideoData.Daos
{
    public class BaseDao: NTVideoEntities
    {
        private static BaseDao baseDao;

        public bool IsDead { get; set; }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsDead = true;
        }

        public static BaseDao instance()
        {
            if (baseDao == null || baseDao.IsDead)
            {
                baseDao = new BaseDao();
                baseDao.IsDead = false;
            }
            return baseDao;
        }

        public static bool checkConnection()
        {
            try
            {
                using (var context = new NTVideoEntities())
                {
                    return context.Database.Exists();
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return false;
            }
        }
    }
}

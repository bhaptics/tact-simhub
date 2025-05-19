using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace bHapticsSimHub.ViewModel
{
    public class bHapticsDB
    {
        private LiteDatabase _bhapticsDb = null;
        private ConnectionString _connectionString = null;
        private readonly object @lock = new object();
        private bool _isInitialized = false;

        public Action Initialized;

        public void Initialize()
        {
            lock (@lock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    InitializeDatabase();
                    _isInitialized = true;
                    Initialized?.Invoke();
                }
                catch (Exception e)
                {
                    SimhubUtils.WriteErrorLog($"Initialize() {e.Message}");
                }
            }
        }

        private void InitializeDatabase()
        {
            SimhubUtils.WriteLog("InitializeDatabase");
            try
            {
                var bHapticsDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "/bHaptics/";
                var dir = new DirectoryInfo(bHapticsDir);
                if (!dir.Exists)
                {
                    dir.Create();
                    SimhubUtils.WriteLog($"InitializeDatabase Directory Created");
                }

                _connectionString = new ConnectionString()
                {
                    Filename = @bHapticsDir + "bHapticsSimhubProfile.bh",
                    Mode = LiteDB.FileMode.Shared,
                };

                _bhapticsDb = new LiteDatabase(_connectionString);
            }
            catch (Exception e)
            {
                SimhubUtils.WriteErrorLog($"InitializeDatabase() Exception {e.Message}");
            }
        }
        public void InsertSimhubProfile(SimhubProfile profile)
        {
            try
            {
                var col = _bhapticsDb.GetCollection<SimhubProfile>("SimhubProfile");
                col.Insert(profile);
            }
            catch (Exception e)
            {
                SimhubUtils.WriteErrorLog($"InsertSimhubProfile() Exception {e.Message}");
            }
        }

        public SimhubProfile GetSimhubProfile(string workspaceId)
        {
            try
            {
                var col = _bhapticsDb.GetCollection<SimhubProfile>("SimhubProfile");
                return col?.FindById(workspaceId);
            }
            catch (Exception e)
            {
                SimhubUtils.WriteErrorLog($"GetSimhubProfile() Exception {e.Message}");
                
            }
            return null;
        }

        public List<SimhubProfile> GetSimhubProfileList()
        {
            try
            {
                var col = _bhapticsDb.GetCollection<SimhubProfile>("SimhubProfile");
                return col?.FindAll().ToList();
            }
            catch (Exception e)
            {
                SimhubUtils.WriteErrorLog($"GetSimhubProfileList() Exception {e.Message}");

            }
            return new List<SimhubProfile>();
        }

        public void UpdateSimhubProfile(string id, SimhubProfile message)
        {
            try
            {
                var col = _bhapticsDb.GetCollection<SimhubProfile>("SimhubProfile");
                if (col == null)
                {
                    return;
                }

                col.Upsert(id, message);
                SimhubUtils.WriteLog("Haptic New Update");
            }
            catch (Exception e)
            {
                SimhubUtils.WriteErrorLog($"UpdateSimhubProfile() Exception {e.Message}");
            }
        }
        public bool RemoveSimhubProfile(string id)
        {
            try
            {
                var col = _bhapticsDb.GetCollection<SimhubProfile>("SimhubProfile");
                return col.Delete(id);
            }
            catch (Exception e)
            {
                SimhubUtils.WriteErrorLog($"RemoveSimhubProfile {e.Message}");
            }

            return false;
        }
    }
}

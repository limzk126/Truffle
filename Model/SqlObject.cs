using System;
using System.Collections.Generic;
using System.Reflection;
using Success.Utils;
using Truffle.Database;
using Truffle.Procedures;

namespace Truffle.Model
{
    public abstract class SqlObject
    {
        public SqlObject() {}

        public SqlObject(object val, DatabaseConnector database)
        {
            string req = BuildRequest(val, GetId());

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{val} was not present in the database");

            LoadValues(response[0]);
        }

        public SqlObject(object val, string key, DatabaseConnector database)
        {
            string req = BuildRequest(val, key);

            var response = (List<Dictionary<string, object>>) database.RunCommand(req, complex:true);
            if (response.Count == 0) throw new KeyNotFoundException($"{key} of value {SqlUtils.Parse(val)} was not present in the database");

            LoadValues(response[0]);
        }

        public SqlObject(Dictionary<string, object> values)
        {
            LoadValues(values);
        }

        public virtual string BuildAllRequest() 
        {
            
            return $"SELECT {BuildColumnSelector()} FROM {GetTable()}";
        }

        public string GetId()
        {
            return GetId(this.GetType());
        }

        public string GetTable()
        {
            return GetTable(this.GetType());
        }

        public virtual Dictionary<string, object> GetAllValues()
        {
            Dictionary<string, object> ans = new Dictionary<string, object>();

            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                ans.Add(attribute.Name, p.GetValue(this));
            }
            return ans;
        }

        public virtual void LogValues()
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                Console.WriteLine($"{p.Name}: {p.GetValue(this)} of type {p.GetType()}");
            }
        }
        public virtual bool Create(DatabaseConnector database) 
        {
            SqlInserter inserter = new SqlInserter(this);
            return inserter.Insert(GetTable(),database);
        }

        public virtual bool Update(DatabaseConnector database) 
        {
            SqlUpdater updater = new SqlUpdater(this);
            return updater.Update(GetTable(),database);
        }

        public void LoadValues(Dictionary<string, object> values)
        {
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                try {
                    var value = values[attribute.Name];
                    if (typeof(System.DBNull).IsInstanceOfType(value))
                    {
                        p.SetValue(this, null);
                        continue;
                    }
                    if (typeof(DateTime).Equals(p.GetType()) && typeof(string).IsInstanceOfType(value))
                    {
                        p.SetValue(this, SqlUtils.ParseDate(value));
                        continue;
                    }
                    p.SetValue(this, value);
                } catch (Exception e)
                {Console.WriteLine(e.Message); Console.WriteLine(e.StackTrace);}
            }
        }

        protected static string GetId(Type t)
        {
            foreach (PropertyInfo p in t.GetProperties())
            {
                var attribute = (IdAttribute) p.GetCustomAttribute(typeof(IdAttribute));
                if (attribute == null) continue;

                var column = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                return column.Name;
            }
            
            return null;
        }

        protected static string GetTable(Type t)
        {
            var table = (TableAttribute) t.GetCustomAttribute(typeof(TableAttribute));
            return table.Name;
        }

        protected string BuildRequest(object raw, string key) 
        {
            string val = SqlUtils.Parse(raw);
            return $"SELECT {BuildColumnSelector()} FROM {GetTable()} WHERE {key}={val}";
        }

        public virtual string BuildColumnSelector()
        {
            List<string> values = new List<string>();
            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                var attribute = (ColumnAttribute) p.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute == null) continue;

                values.Add(attribute.Name);
            }

            return String.Join(",", values);
        }
    }
}
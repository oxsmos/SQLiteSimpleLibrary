using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Data.Linq.Mapping;
using System.Collections.Generic;

namespace SQLiteSimpleLibrary
{
  public class SQLiteSL
  {
    public struct db
    {
      public static string dataSource { internal get; set; } = "demo.s3db";
      public static string version { internal get; set; } = "3";
      public static string pooling { internal get; set; } = "True";
      public static string maxPool { internal get; set; } = "50";
    }

    public sealed class Connection
    {
      internal SQLiteConnection sqliteConnection;

      private static SQLiteConnection Init()
      {
        SQLiteConnection sqliteConnection =
          new SQLiteConnection($@"Data Source={db.dataSource}; Version={db.version};
                                Pooling={db.pooling}; Max Pool Size={db.maxPool};");

        return sqliteConnection;
      }

      public void Open()
      {
        sqliteConnection = Init();
        sqliteConnection.Open();
      }

      public static void Close(SQLiteConnection sqliteConnection)
      {
        sqliteConnection.Close();
      }
    }

    public DbDataReader DataReader(string query)
    {
      DbDataReader reader = null;

      try
      {
        Connection conn = new Connection();

        conn.Open();

        SQLiteCommand sqliteCommand = new SQLiteCommand(query, conn.sqliteConnection);

        reader = sqliteCommand.ExecuteReader();

        return reader;
      }
      catch (Exception ex)
      {
        throw new Exception("Error on DataReader", ex);
      }
    }

    public object DataReader(string table, string field)
    {
      object tab = null;
      SQLiteDataReader reader = null;

      try
      {
        field = field != null ? $"{field.Replace("'", "''")}" : "NULL";
        string command = $"SELECT * FROM {table} WHERE {field.Replace("'", "''")} = (SELECT MAX({field}) FROM {table}) COLLATE NOCASE;";
        tab = new List<object>();
        List<object> obj;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          conn.Open();
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            reader = sqliteCommand.ExecuteReader();

            do
            {
              obj = new List<object>();
              foreach (object o in reader)
                obj.Add(o);
              tab = obj;
            } while (reader.Read());

          }
        }

        return obj[0];
      }
      catch (Exception ex)
      {
        throw new Exception("Error on DataReader", ex);
      }
    }

    public object DataReader(string table, string field, string data)
    {
      object tab = null;

      SQLiteDataReader reader = null;

      try
      {
        data = data != null ? $"'{data.Replace("'", "''")}'" : "NULL";
        string command = $"SELECT * FROM {table} WHERE {field} Like {data} COLLATE NOCASE;";
        tab = new List<object>();
        List<object> obj;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            reader = sqliteCommand.ExecuteReader();

            if (reader.HasRows == true)
              do
              {
                obj = new List<object>();
                foreach (object o in reader)
                  obj.Add(o);
                tab = obj;
              } while (reader.Read());
            else
              throw new Exception("non trovato");
          }
        }
        return obj[0];
      }
      catch (Exception ex)
      {
        ex.Data.Add(field, data);
        throw ex;
      }
    }

    public object DataReader(string table, string field, string data, string field1, string data1)
    {
      object tab = null;
      SQLiteDataReader reader = null;

      try
      {
        data = data != null ? $"'{data.Replace("'", "''")}'" : "NULL";
        data1 = data1 != null ? $"'{data1.Replace("'", "''")}'" : "NULL";
        string command = $"SELECT * FROM {table} WHERE {field} Like {data} AND {field1} Like {data1} COLLATE NOCASE;";
        tab = new object();
        List<object> obj;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            reader = sqliteCommand.ExecuteReader();

            obj = new List<object>();
            foreach (object o in reader)
              obj.Add(o);
            tab = obj[0];
          }
        }

        return tab;
      }
      catch (Exception ex)
      {
        throw new Exception("Error on DataReader", ex);
      }
    }

    public bool Belongs(string table, string field, string data, string fieldCompare, string checkTo)
    {
      object tab = null;
      SQLiteDataReader reader = null;
      string fieldData = null;

      try
      {
        data = data != null ? $"'{data.Replace("'", "''")}'" : "NULL";
        string command = $"SELECT {fieldCompare} FROM {table} WHERE {field} Like {data} COLLATE NOCASE;";
        tab = new List<object>();

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            reader = sqliteCommand.ExecuteReader();

            reader.Read();
            fieldData = reader[fieldCompare].ToString();
          }
        }

        return fieldData == checkTo;
      }
      catch (Exception ex)
      {
        throw new Exception("Error on DataReader", ex);
      }
    }

    public virtual long Insert(string table, string column, string value)
    {
      try
      {
        value = value != null ? $"'{value.Replace("'", "''")}'" : "NULL";
        string command = $"INSERT INTO {table} ({column}) VALUES({value});";
        int rowsAffected = 0;
        long lastId = 0;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
              rowsAffected = sqliteCommand.ExecuteNonQuery();
              transaction.Commit();
            }
          }
        }

        if (rowsAffected > 0)
        {
          command = @"select last_insert_rowid()";
        }
        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
              lastId = (long)sqliteCommand.ExecuteScalar();
              transaction.Commit();
            }
          }
        }
        return lastId;
      }
      catch (Exception ex)
      {
        throw new Exception("Error on Update", ex);
      }
    }

    public virtual long Update(string table, string column, string value, string idColumn, long index)
    {
      try
      {
        value = value != null ? $"'{value.Replace("'", "''")}'" : "NULL";
        string command = $@"UPDATE {table} SET {column} = {value} WHERE {idColumn} = {index};";
        int rowsAffected = 0;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
              rowsAffected = sqliteCommand.ExecuteNonQuery();
              transaction.Commit();
            }
          }
        }

        return rowsAffected;
      }
      catch (Exception ex)
      {
        throw new Exception($"Error on Update of {table}", ex);
      }
    }

    public virtual long Update(string table, string column, string value, string fieldFind, string fieldIs)
    {
      try
      {
        value = value != null ? $"'{value.Replace("'", "''")}'" : "NULL";
        fieldIs = fieldIs != null ? $"'{fieldIs.Replace("'", "''")}'" : "NULL";
        string command = $@"UPDATE {table} SET {column} = {value} WHERE {fieldFind} = {fieldIs};";
        int rowsAffected = 0;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
              rowsAffected = sqliteCommand.ExecuteNonQuery();
              transaction.Commit();
            }
          }
        }

        return rowsAffected;
      }
      catch (Exception ex)
      {
        throw new Exception($"Error on Update of {table}", ex);
      }
    }

    public virtual long DeleteAllRecords(string table)
    {
      try
      {
        string command = $@"DELETE FROM {table}; DELETE FROM sqlite_sequence WHERE name='{table}';";
        int rowsAffected = 0;

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
              rowsAffected = sqliteCommand.ExecuteNonQuery();
              transaction.Commit();
            }
          }
        }

        return rowsAffected;
      }
      catch (Exception ex)
      {
        throw new Exception($"Error on Deletion of records in {table}", ex);
      }
    }

    public virtual void Vacuum()
    {
      try
      {
        string command = $@"VACUUM;";

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
              transaction.Commit();
            }
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Errore on vaccum of database", ex);
      }
    }

    public List<tab_ISO_3166_2> ISO_3166_Reader()
    {
      List<tab_ISO_3166_2> tabISO = new List<tab_ISO_3166_2>();

      tab_ISO_3166_2 tabiso = new tab_ISO_3166_2();

      SQLiteDataReader reader = null;

      try
      {
        string command = "SELECT * FROM tab_comuni_istat;";

        using (SQLiteConnection conn = new Connection().sqliteConnection)
        {
          using (SQLiteCommand sqliteCommand = new SQLiteCommand(command, conn))
          {
            reader = sqliteCommand.ExecuteReader();
          }
        }

        while (reader.Read())
        {
          tabISO.Add(new tab_ISO_3166_2
          {
            id = reader[0],
            nazione = reader[5],
            codice = reader[3],
            alpha2 = reader[1],
            alpha3 = reader[2],
            localISO = reader[4],
          });
        }

        return tabISO;
      }
      catch (Exception ex)
      {
        throw new Exception("Exception on DataReader", ex);
      }
    }

    [Table(Name = "tab_ISO_3166_2")]
    public class tab_ISO_3166_2
    {
      private int _id;
      [Column(IsPrimaryKey = true, Storage = "id")]
      public object id
      {
        get
        {
          return this._id;
        }
        set
        {
          this._id = Convert.ToInt32(value);
        }
      }

      private string _nazione;
      [Column(Storage = "nazione")]
      public object nazione
      {
        get
        {
          return this._nazione;
        }
        set
        {
          this._nazione = Convert.ToString(value);
        }
      }

      private string _codice;
      [Column(Storage = "codice")]
      public object codice
      {
        get
        {
          return this._codice;
        }
        set
        {
          this._codice = Convert.ToString(value);
        }
      }

      private string _alpha2;
      [Column(Storage = "alpha2")]
      public object alpha2
      {
        get
        {
          return this._alpha2;
        }
        set
        {
          this._alpha2 = Convert.ToString(value);
        }
      }

      private string _alpha3;
      [Column(Storage = "alpha3")]
      public object alpha3
      {
        get
        {
          return this._alpha3;
        }
        set
        {
          this._alpha3 = Convert.ToString(value);
        }
      }

      private string _localISO;
      [Column(Storage = "localISO")]
      public object localISO
      {
        get
        {
          return this._localISO;
        }
        set
        {
          this._localISO = Convert.ToString(value);
        }
      }
    }

    public void ConnectionStringBuilder()
    {
      SQLiteConnectionStringBuilder sqliteConnectionStringBuilder = new SQLiteConnectionStringBuilder();
    }
  }
}

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedTyperDataAccess
{
    public class LevelAccessor
    {
        public static decimal RetrieveWPMXPModifier(decimal WPM)
        {
            decimal xpModifier = 1;

            var conn = DBConnection.GetConnection();
            var cmdText = @"sp_retrieve_wpm_xp_modifier";
            var cmd = new SqlCommand(cmdText, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@WPM", SqlDbType.Decimal);
            cmd.Parameters["@WPM"].Value = WPM;

            try
            {
                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();
                    xpModifier = reader.GetDecimal(0);
                }
                reader.Close();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                conn.Close();
            }

            return xpModifier;
        }

        public static decimal RetrieveTimeXPModifier(decimal secondsElapsed)
        {
            decimal xpModifier = 1;

            var conn = DBConnection.GetConnection();
            var cmdText = @"sp_retrieve_time_xp_modifier";
            var cmd = new SqlCommand(cmdText, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@SecondsElapsed", SqlDbType.Decimal);
            cmd.Parameters["@SecondsElapsed"].Value = secondsElapsed;

            try
            {
                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();
                    xpModifier = reader.GetDecimal(0);
                }
                reader.Close();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                conn.Close();
            }

            return xpModifier;
        }


    }
}

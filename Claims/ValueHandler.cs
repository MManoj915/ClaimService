using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimService
{
    class ValueHandler
    { 
        public static DateTime ToClaimDate(object value)
        {
            DateTime result = new DateTime(1, 1, 1);

            DateTime.TryParseExact(DBtoString(value), new string[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

            return result;
        }

        public static DateTime DBToClaimDate(object value)
        {
            DateTime result = new DateTime(1, 1, 1);

            DateTime.TryParseExact(DBtoString(value), new string[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

            return result;
        }

        public static string DBtoString(object Value)
        {
            if (Value != null && Value != DBNull.Value)
                return Convert.ToString(Value);
            return "";
        }

        public static bool IsEmpty(string Value)
        {
            if (Value == null)
                return true;
            return Value.ToString().Trim() == "";
        }

        public static object stringToDB(string Value)
        {
            if (Value != null)
                if (Value.Trim() != "")
                    return Value;
            return DBNull.Value;
        }

        public static object dateToDB(DateTime Value)
        {
            if (Value != null && Value != new DateTime(1, 1, 1))
                return Value;
            return DBNull.Value;
        }

        public static DateTime DBToDate(object Value)
        {
            if (Value != null && Value != DBNull.Value)
                return Convert.ToDateTime(Value);
            return new DateTime(1, 1, 1);
        } 

        public static int DBtoInteger(object Value, bool returnZeroWhenNull)
        {
            int ret = (returnZeroWhenNull ? 0 : -1);
            if (Value != null && Value != DBNull.Value)
                ret = Convert.ToInt32(Value);

            return ret;
        }

        public static int DBtoInteger(object Value)
        {
            return DBtoInteger(Value, false);
        }

        public static double DBtoDouble(object Value)
        {
            if (Value != null && Value != DBNull.Value)
                return Convert.ToDouble(Value);
            return -1;
        }

        public static bool DBtoBool(object Value)
        {
            if (Value != null && Value != DBNull.Value)
            {
                string temp = Convert.ToString(Value);
                if (temp == "Y" || temp == "Yes" || temp == "1")
                    return true;
            }
            return false;
        } 

        public static bool hasValue(object obj)
        {
            return obj != null;
        }

        public static bool hasValue(string obj)
        {
            return obj.Trim() != string.Empty;
        }

        public static bool isValidEmail(string EmailAddress)
        {
            if (EmailAddress == "" || EmailAddress == null)
                return true;
            try
            {
                System.Net.Mail.MailAddress m = new System.Net.Mail.MailAddress(EmailAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object ConditionalResult(bool Condition, object TrueResult, object FalseResult)
        {
            if (Condition)
                return TrueResult;
            return FalseResult;
        }
    }
}

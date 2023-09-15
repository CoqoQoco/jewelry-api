using System;

namespace jewelry.Model.Exceptions
{
    public class HandleException : Exception
    {
        public HandleException(string message) : base(message)
        {
        }

        public HandleException(string ErrorMsg, string TargetError) : base(message: ReturnErrorMsg(ErrorMsg, TargetError))
        {
        }

        public static string ReturnErrorMsg(string ErrorMsg, string TargetError)
        {
            string messages = string.Format(ErrorMsg, TargetError);
            return string.Join(Environment.NewLine, messages);
        }

    }
}

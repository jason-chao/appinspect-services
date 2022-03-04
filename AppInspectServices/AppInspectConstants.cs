using System.Linq;
using System.Reflection;

namespace AppInspectServices
{
    static public class AppInspectConstants
    {

        static public class TaskActions
        {
            public const string query_googleplay = "query_googleplay";
            public const string retrieve_apk = "retrieve_apk";
            public const string convert_and_move_apk = "convert_and_move_apk";
            public const string analyse_apks = "analyse_apks";
        }

        static public class RPCGroups
        {
            public const string Workers = "Workers";
        }

        static public class RPCClientMethods
        {
            public const string WorkerRegistered = "WorkerRegistered";
            public const string AssignTask = "AssignTask";
            public const string TaskCreated = "TaskCreated";
            public const string PendingTasks = "PendingTasks";
        }

        static public bool Exists(string value, Type objectType)
        {
            // for this method to work, all property names must equal to their values
            // only the constant classes here are supported
            var supportedTypes = new Type[] { typeof(TaskActions), typeof(RPCGroups), typeof(RPCClientMethods) };

            if (!supportedTypes.Contains(objectType))
                throw new Exception("Object type is not supported for the check");

            foreach (var memberInfo in objectType.GetMembers())
            {
                if (memberInfo.Name == value)
                    return true;
            }
            return false;
        }
    }
}

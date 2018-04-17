using everis.Ees.Proxy.Core;
using everis.Ees.Proxy.Services;
using everis.Ees.Proxy.Services.Interfaces;
using Everis.Ees.Entities.Enums;
using Everis.Ees.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Robot.Util
{
    public static class ExecFunctionLog<T> where T : IRobot, new()
    {
        private static BaseRobot<T> BaseRobotUtil = null;
        public static void SetBaseRobotLocal(BaseRobot<T> robotAux)
        {
            BaseRobotUtil = robotAux;
        }

        public static void ExecFunc(string mensagem, Action method)
        {
            try
            {
                method();
            }
            catch (Exception ex)
            {

                if (BaseRobotUtil == null)
                {
                    throw new Exception("Error inicialización ExecFunctionLog/BaseRobot.");
                }
             
                StackTrace stackTrace = new StackTrace();
                var container = ODataContextWrapper.GetContainer();
                LogData logData = new LogData();
                var lgm = container.LogMessages.Where(x => x.Id == 47);
                logData.LogLevel = LogLevel.Process;
                logData.ErrorType = ErrorType.Exception;
                logData.Method = stackTrace.GetFrame(1).GetMethod().Name;
                logData.Parameters = new System.Collections.ObjectModel.ObservableCollection<KeyValueModel>();
                string strEstado = Utils.GetKeyFromConfig("nomEstado") != null ? Utils.GetKeyFromConfig("nomEstado") : string.Empty;
                string strGuid = Utils.GetKeyFromConfig("robotGuid") != null ? Utils.GetKeyFromConfig("robotGuid") : string.Empty;
                logData.Parameters.Add(new KeyValueModel() { Key = "strMsg", Value = mensagem });
                logData.Parameters.Add(new KeyValueModel() { Key = "nomEstado", Value = strEstado });
                logData.CreationDate = DateTime.Now;
                if (BaseRobotUtil.StateFields != null && BaseRobotUtil.StateFields.Count > 0)
                {
                    logData.StateId = BaseRobotUtil.StateFields.First().StateId;
                    if (BaseRobotUtil.StateFields.First() != null)
                    {
                        logData.State = BaseRobotUtil.StateFields.First().State;
                    }
                }
                logData.Exception = ex.Message;
                logData.StackTrace = ex.ToString();
                logData.LogMessageId = 47;
                logData.RobotVirtualMachineId = new Guid(strGuid);
               
                if (lgm != null && lgm.Count() > 0)
                {
                    logData.LogMessage = lgm.First();
                    logData.Description = string.Format(logData.LogMessage.MessageLog.Replace("strMsg", "0").Replace("nomEstado", "1"), mensagem, strEstado);
                }
                else
                {
                    logData.Description = string.Format("{0} estado de ejecución: {1}", mensagem, strEstado);
                }

                container.AddToLogDatas(logData);
                container.SaveChanges();

                throw new Exception(logData.Description);

            }
        }

      
        }
    }


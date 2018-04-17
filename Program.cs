using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Data.SqlClient;
using System.Reflection;
using Robot.Util;
using System.IO;

namespace Robo.Pedicoes
{
    //Estrutura da lista
    struct ListElement
    {
        public string cliente;
        public string linea;
        public string celda;
        public string tipo;
        public string servicio;
        public string centro;
        public string peticion;
        public string estado;
        public string horas;
        public string descricion;
        public string responsavel;

    }





    public class Program : IRobot
    {
        //lista Global
         private static List<ListElement> listaTader = new List<ListElement>();
        private static double[,] matrizAD = new double[10000,6];
        private static string destino = string.Empty;
        private static string localArquivo = string.Empty;





        private static BaseRobot<Program> _robot = null;
         

        private static void Main(string[] args)
        {
           
                //Inicializando instancia do Robô
                _robot = new BaseRobot<Program>(args);
            //iniciando a function de log
            ExecFunctionLog<Program>.SetBaseRobotLocal(_robot);
            //Inserindo parâmentros para mensageria
            _robot.InsertParamLogMessage(Utils.GetKeyFromConfig(Constants.KEY_LOG_MSG_NAME_STATE), Utils.GetKeyFromConfig(Constants.ROBOT_NAME));
            //Parametrização de leitura de tickets
            _robot.returnAllTickets = false;
            
            _robot.Start();
        }

        protected override void Start()
        {
            //Lista Petições
            List<string> list_ComP = new List<string>();
            List<string> list_SemP = new List<string>();
            //parametros Upload

            destino = _robot.GetValueParamRobot("PastaDestino").ValueParam;
            localArquivo = _robot.GetValueParamRobot("PastaOrigem").ValueParam;

            try
            {
       

                ExecFunctionLog<Program>.ExecFunc("Erro ao Consultar o SQL", () => { SQLCall(); });

                ExecFunctionLog<Program>.ExecFunc("Erro ao criar a lista de novas petições", () => { list_SemP = ValidaPedicao(true); });
                ExecFunctionLog<Program>.ExecFunc("Erro ao criar a lista de petições de atualização de tickets", () => { list_ComP = ValidaPedicao(false); });

                ExecFunctionLog<Program>.ExecFunc("Erro ao criar novos tickets", () => { GerarTicketComp(list_ComP); });
                ExecFunctionLog<Program>.ExecFunc("Erro ao criar novos tickets", () => { GerarTicketSemp(list_SemP); });
                ExecFunctionLog<Program>.ExecFunc("Erro ao inserir o auditor", () => { Auditor(list_SemP, list_ComP); });
               
            }
            catch (Exception ex)
            {
                LogFailProcess(Constants.MSG_ERROR_EVENT_PROCESS_KEY, ex);

            }

        }
        public void SQLCall()
        {
            using (SqlConnection connection = new SqlConnection(@"Data Source=UDI-DDXR862\SRV_EVS_UDI_BI;Initial Catalog=DB_EVERIS;Integrated Security=True"))
            {


                connection.Open();
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT CLIENTE,LINEA,CELDA,TIPO_PETICION,SERVICIO,CENTRO,PETICION,ESTADO,HORAS_ACUERDO,DESCRIPCION_FLUJO,RESPONSABLE_FUNCIONAL" + " FROM FNX.TB_PETICAO" + " WHERE HORAS_ACUERDO > 50 AND ESTADO IN ('EN_EJECUCION','ENTREGADA') AND PETICION_OT = 'PET' AND CENTRO='DMF Uberlândia' AND TIPO_PETICION IN ('MANTENIMIENTO_EVOLUTIVO','MANTENIMIENTO_CORRECTIVO','SPRINT_AGILE') AND (FECHA_REAL_ENTREGA > '2017-11-06' OR FECHA_REAL_ENTREGA IS NULL) AND SERVICIO NOT IN ('Testing','Estimación') AND PETICION >= 1014814 AND PETICION NOT IN (1061734,1061343,1060854,1059479,1058252,1058247,1057656,1057266,1056946,1054049,1053529,1053325,1053125,1052011,1050165,1049304,1048514,1047975,1047855,1047851,1047578,1045712,1045604,1042862,1042281,1041261,1040753,1040159,1039465,1039046,1037710,1037592,1036527,1035023,1034051,1033966,1032443,1031883,1029565,1029405,1028103,1026942,1026453,1026421,1024667,1024208,1023865,1018888,1018494,1018393,1018133,1018126,1016702,1015348,1015346,1015244,1014967)");
                String sql = sb.ToString();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            listaTader.Add(new ListElement()
                            {
                                cliente = reader.GetValue(0).ToString(),
                                linea = reader.GetValue(1).ToString(),
                                celda = reader.GetValue(2).ToString(),
                                tipo = reader.GetValue(3).ToString(),
                                servicio = reader.GetValue(4).ToString(),
                                centro = reader.GetValue(5).ToString(),
                                peticion = reader.GetValue(6).ToString(),
                                estado = reader.GetValue(7).ToString(),
                                horas = reader.GetValue(8).ToString(),
                                descricion = reader.GetValue(9).ToString(),
                                responsavel = reader.GetValue(10).ToString()
                            });

                        }
                    }

                }

            }
        }
        public void GerarTicketSemp(List<string> list_SemP)
        {
            foreach (string id in list_SemP)
            {
                var linha = listaTader.Where(x => x.peticion.ToString() == id).FirstOrDefault();
                var container = ODataContextWrapper.GetContainer();
                try
                {
                    

                       var Tservicio = container.DomainValues.Where(x => x.Value == linha.servicio).FirstOrDefault().Id;
                    var Tcliente = container.DomainValues.Where(x => x.Value == linha.cliente).FirstOrDefault().Id;
                    var Testado = container.DomainValues.Where(x => x.Value == linha.estado).FirstOrDefault().Id;
                    var Tlinea = container.DomainValues.Where(x => x.Value == linha.linea).FirstOrDefault().Id;
                    var Tcelda = container.DomainValues.Where(x => x.Value == linha.celda).FirstOrDefault().Id;
                    var Ttipo = container.DomainValues.Where(x => x.Value == linha.tipo).FirstOrDefault().Id;
                    // Por hora não existe dominio de responsaveis então esta sendo colocado a string
                    // var IdResponsavel = container.DomainValues.Where(x => x.Value == linha.responsavel).FirstOrDefault().Id;
                }
                catch(Exception)
                {
                    string msg = "Não consegiu encontrar o value nos dominios da petição: " + linha.peticion;
                    throw new Exception(msg);
                    
                }
                
                Ticket obj = new Ticket
                {
                    CreationDate = DateTime.Now,
                    StateId = 3,
                    Description = id

                };
                #region Cabeçalho
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 236,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = id
                });

                var Idservicio = container.DomainValues.Where(x => x.Value == linha.servicio).FirstOrDefault().Id;
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 242,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idservicio.ToString()
                });
             
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 232,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = linha.centro.ToString()
                });

                var Idcliente = container.DomainValues.Where(x => x.Value == linha.cliente).FirstOrDefault().Id;
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 233,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value =  Idcliente.ToString()
                });
                var Idestado = container.DomainValues.Where(x => x.Value == linha.estado).FirstOrDefault().Id;
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 240,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idestado.ToString()
                });
              
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 243,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = linha.horas.ToString()
                });
                var Idlinea = container.DomainValues.Where(x => x.Value == linha.linea).FirstOrDefault().Id;
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 238,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idlinea.ToString()
                });
                var Idcelda = container.DomainValues.Where(x => x.Value == linha.celda).FirstOrDefault().Id;
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 239,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idcelda.ToString()
                });
                var Idtipo = container.DomainValues.Where(x => x.Value == linha.tipo).FirstOrDefault().Id;
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 241,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idtipo.ToString()
                });
                
                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 235,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = linha.responsavel
                });

                if(linha.estado=="CERRADA")
                    obj.TicketValues.Add(new TicketValue
                    {
                        CreationDate = DateTime.Now,
                        FieldId = 249,
                        ClonedValueOrder = null,
                        TicketId = obj.Id,
                        Value = "FINALIZADA"
                    });

                #endregion

                string categoria = "";
                if ((linha.tipo == "MANTENIMIENTO_EVOLUTIVO" || linha.tipo== "MANTENIMIENTO_CORRECTIVO") && linha.descricion == "Gestión Requerimiento")
                    categoria = "REQ";

                if ((linha.tipo == "MANTENIMIENTO_EVOLUTIVO" || linha.tipo == "MANTENIMIENTO_CORRECTIVO") && linha.descricion == "Gestión ACC")
                    categoria = "ACC";

                if (linha.tipo == "SPRINT_AGILE")
                    categoria = "Agile";

                if (categoria == "ACC")
                {
                    PreencherCategoriaACC(false, obj);
                    categoria = "277";
                }
                else
                if (categoria == "Agile")
                {
                    PreencherCategoriaAgile(false, obj);
                    categoria = "278";
                }
                else
                {
                    PreencherCategoriaReq(false, obj);
                    categoria = "279";
                }

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 244,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = categoria
                });
                #region Nomenclatura Excel
                string partialName = "EstadoPET"+ "_"+linha.peticion+"_"; 
                DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo(localArquivo);
                FileInfo[] filesInDir = hdDirectoryInWhichToSearch.GetFiles("*" + partialName + "*.*");
                string fullName = "";
                int comp_1 = 0;
                int comp_2 = 0;
                foreach (FileInfo foundFile in filesInDir)
                {
                    string name = foundFile.FullName;

                    int letras = name.Length;

                    letras = letras - 8;

                    comp_1 =  Convert.ToInt32(name.Substring(letras, 8));

                    if(comp_1> comp_2)
                    {
                        comp_2 = comp_1;
                        fullName = name;
                    }

                }
                #endregion

                if (fullName!="")
                UploadFile(linha.peticion, destino, localArquivo, obj);

                _robot.SaveNewTicket(obj);


            }


        }
        public void GerarTicketComp(List<string> list_ComP)
        {
            foreach (string id in list_ComP)
            {
                Ticket obj = _robot.Tickets.Where(x => x.Description.ToString() == id).First();
                var flag = obj.TicketValues.Where(x => x.FieldId == 249).FirstOrDefault();
                if (flag.Value == "FINALIZADA")
                    _robot.SaveTicketNextState(obj, 10);

                var linha = listaTader.Where(x => x.peticion.ToString() == id).First();
                #region Cabeçalho
                var container = ODataContextWrapper.GetContainer();
                


                var Idservicio = container.DomainValues.Where(x => x.Value == linha.servicio).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 242,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idservicio.ToString()
                });

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 232,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = linha.centro.ToString()
                });

                var Idcliente = container.DomainValues.Where(x => x.Value == linha.cliente).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 233,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idcliente.ToString()
                });

                var Idestado = container.DomainValues.Where(x => x.Value == linha.estado).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 240,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idestado.ToString()
                });

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 243,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = linha.horas.ToString()
                });

                var Idlinea = container.DomainValues.Where(x => x.Value == linha.linea).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 238,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idlinea.ToString()
                });

                var Idcelda = container.DomainValues.Where(x => x.Value == linha.celda).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 239,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idcelda.ToString()
                });

                var Idtipo = container.DomainValues.Where(x => x.Value == linha.tipo).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 241,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = Idtipo.ToString()
                });

                var IdResponsavel = container.DomainValues.Where(x => x.Value == linha.responsavel).First().Id;

                obj.TicketValues.Add(new TicketValue
                {
                    CreationDate = DateTime.Now,
                    FieldId = 235,
                    ClonedValueOrder = null,
                    TicketId = obj.Id,
                    Value = IdResponsavel.ToString()
                });


                #endregion
                string categoria = obj.TicketValues.FirstOrDefault(x => x.FieldId ==244).Value.ToString();

                if (categoria == "ACC")
                {
                    PreencherCategoriaACC(true, obj);


                }
                else
                if (categoria == "Agile")
                {
                    PreencherCategoriaAgile(true, obj);

                }
                else
                {
                    PreencherCategoriaReq(true, obj);

                }

                #region Nomenclatura Excel
                string partialName = "EstadoPET" + "_" + linha.peticion + "_";
                DirectoryInfo hdDirectoryInWhichToSearch = new DirectoryInfo(localArquivo);
                FileInfo[] filesInDir = hdDirectoryInWhichToSearch.GetFiles("*" + partialName + "*.*");
                string fullName = "";
                int comp_1 = 0;
                int comp_2 = 0;
                foreach (FileInfo foundFile in filesInDir)
                {
                    string name = foundFile.FullName;

                    int letras = name.Length;

                    letras = letras - 8;

                    comp_1 = Convert.ToInt32(name.Substring(letras, 8));

                    if (comp_1 > comp_2)
                    {
                        comp_2 = comp_1;
                        fullName = name;
                    }

                }
                #endregion

                if (fullName != "")
                    UploadFile(linha.peticion, destino, localArquivo, obj);


                _robot.SaveTicketNextState(obj, 8);

            }


        }
        public void PreencherCategoriaReq(bool flag, Ticket obj)
        {
            //Sem petições
            if (flag==false)
            {
                
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 156, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 157, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 158, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 159, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 160, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 161, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 162, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 163, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 164, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 165, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 166, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 167, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 168, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 169, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 170, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 171, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 172, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 173, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 174, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 175, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 176, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 177, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 178, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 179, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 180, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 181, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 182, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 183, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 184, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 185, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 186, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 187, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 188, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 189, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 190, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 191, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 192, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 193, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 194, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 195, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 196, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 197, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 198, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 199, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 200, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 201, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 202, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 203, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 204, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 205, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 206, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 207, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 208, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 209, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 210, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 211, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 212, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 213, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 214, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 215, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 216, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 217, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 218, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 219, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 220, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 221, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 222, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 223, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 224, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 225, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 226, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 227, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 228, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                DateTime dt = DateTime.Now;
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 237, ClonedValueOrder = 0, TicketId = obj.Id, Value = String.Format("{0:dd/MM/yyyy}", dt) });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 248, ClonedValueOrder = null, TicketId = obj.Id, Value = "1", });
                


            }
            //Com petições
            else
            {
                int contador = Convert.ToInt32(obj.TicketValues.FirstOrDefault(x => x.FieldId == 248).Value);
                DateTime dt = DateTime.Now;
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 237, ClonedValueOrder = contador, TicketId = obj.Id, Value = String.Format("{0:dd/MM/yyyy}", dt) });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 156, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 157, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 158, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 159, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 160, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 161, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 162, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 163, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 164, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 165, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 166, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 167, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 168, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 169, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 170, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 171, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 172, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 173, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 174, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 175, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 176, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 177, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 178, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 179, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 180, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 181, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 182, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 183, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 184, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 185, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 186, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 187, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 188, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 189, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 190, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 191, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 192, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 193, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 194, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 195, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 196, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 197, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 198, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 199, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 200, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 201, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 202, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 203, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 204, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 205, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 206, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 207, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 208, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 209, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 210, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 211, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 212, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 213, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 214, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 215, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 216, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 217, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 218, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 219, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 220, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 221, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 222, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 223, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 224, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 225, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 226, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 227, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 228, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 248, ClonedValueOrder = null, TicketId = obj.Id, Value = (contador + 1).ToString(), });

            }


        }
        public void PreencherCategoriaAgile(bool flag, Ticket obj)

        {
            //Sem petição
            if (flag == false)
            {
                DateTime dt = DateTime.Now;
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 237, ClonedValueOrder = 0, TicketId = obj.Id, Value = String.Format("{0:dd/MM/yyyy}", dt) });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 79, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 80, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 81, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 82, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 83, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 84, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 85, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 86, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 87, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 88, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 89, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 90, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 91, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 92, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 93, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 94, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 95, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 96, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 97, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 98, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 99, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 100, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 101, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 102, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 103, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 104, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 105, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 106, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 107, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 108, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 109, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 110, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 111, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 112, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 113, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 114, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 115, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 116, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 117, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 118, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 119, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 120, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 121, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 122, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 123, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 124, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 125, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 126, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 127, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 128, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 129, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 130, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 131, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 132, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 133, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 134, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 135, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 136, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 137, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 138, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 139, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 140, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 141, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 142, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 143, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 144, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 145, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 146, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 147, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 148, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 149, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 150, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 151, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 152, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 153, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 154, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 155, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 248, ClonedValueOrder = null, TicketId = obj.Id, Value = "1", });



            }
            //Com petição
            else
            {
                int contador = Convert.ToInt32(obj.TicketValues.FirstOrDefault(x => x.FieldId == 248).Value);
                DateTime dt = DateTime.Now;
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 237, ClonedValueOrder = contador, TicketId = obj.Id, Value = String.Format("{0:dd/MM/yyyy}", dt) });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 79, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 80, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 81, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 82, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 83, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 84, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 85, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 86, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 87, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 88, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 89, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 90, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 91, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 92, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 93, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 94, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 95, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 96, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 97, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 98, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 99, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 100, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 101, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 102, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 103, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 104, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 105, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 106, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 107, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 108, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 109, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 110, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 111, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 112, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 113, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 114, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 115, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 116, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 117, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 118, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 119, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 120, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 121, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 122, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 123, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 124, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 125, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 126, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 127, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 128, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 129, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 130, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 131, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 132, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 133, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 134, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 135, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 136, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 137, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 138, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 139, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 140, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 141, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 142, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 143, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 144, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 145, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 146, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 147, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 148, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 149, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 150, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 151, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 152, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 153, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 154, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 155, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 248, ClonedValueOrder = null, TicketId = obj.Id, Value = (contador + 1).ToString(), });



            }





        }
        public void PreencherCategoriaACC(bool flag, Ticket obj)
        {
           //Sem petição
            if (flag == false)
            {
                DateTime dt = DateTime.Now;
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 237, ClonedValueOrder = 0, TicketId = obj.Id, Value = String.Format("{0:dd/MM/yyyy}", dt) });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 12, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 13, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 14, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 15, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 16, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 17, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 18, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 19, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 20, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 21, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 22, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 23, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 24, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 25, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 26, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 27, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 28, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 29, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 30, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 31, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 32, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 33, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 34, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 35, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 36, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 37, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 38, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 39, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 40, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 41, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 42, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 43, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 44, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 45, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 46, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 47, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 48, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 49, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 50, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 51, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 52, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 53, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 54, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 55, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 56, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 57, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 58, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 59, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 60, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 61, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 62, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 63, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 64, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 65, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 66, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 67, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 68, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 69, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 70, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 71, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 72, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 73, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 74, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 75, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 76, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 77, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 78, ClonedValueOrder = 0, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 248, ClonedValueOrder = null, TicketId = obj.Id, Value = "1", });
            }
            //Com Petição
            else
            {
                int contador = Convert.ToInt32(obj.TicketValues.FirstOrDefault(x => x.FieldId == 248).Value);
                DateTime dt = DateTime.Now;
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 237, ClonedValueOrder = contador, TicketId = obj.Id, Value = String.Format("{0:dd/MM/yyyy}", dt) });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 12, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 13, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 14, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 15, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 16, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 17, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 18, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 19, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 20, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 21, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 22, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 23, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 24, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 25, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 26, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 27, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 28, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 29, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 30, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 31, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 32, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 33, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 34, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 35, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 36, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 37, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 38, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 39, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 40, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 41, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 42, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 43, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 44, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 45, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 46, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 47, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 48, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 49, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 50, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 51, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 52, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 53, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 54, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 55, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 56, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 57, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 58, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 59, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 60, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 61, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 62, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 63, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 64, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 65, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 66, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 67, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 68, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 69, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 70, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 71, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 72, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 73, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 74, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 75, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 76, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 77, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 78, ClonedValueOrder = contador, TicketId = obj.Id, Value = "", });
                obj.TicketValues.Add(new TicketValue { CreationDate = DateTime.Now, FieldId = 248, ClonedValueOrder = null, TicketId = obj.Id, Value = (contador+1).ToString(), });

            }



        }
        public List<string> ValidaPedicao(bool flag)
        {
            List<string> listaretorno = new List<string>();

            //Separa a linha da lista
            foreach (var linha in listaTader)
            {
                //numero petição
                string numeroTader = linha.peticion.ToString();
                //Set ListaValidada em forma de Lista


                //valida o tipo de lista
                

                var  listaValidada = (_robot.GetDataQueryTicket().Where(x => x.Description == numeroTader && x.StateId == _robot.RobotState.Id).FirstOrDefault()) == null ? null: numeroTader;

                if (flag == false && listaValidada != null)
                {
                    
                        listaretorno.Add(listaValidada);

                        listaValidada = null;

                }
                else 
                if ( flag == true && listaValidada== null)
                {
                    listaretorno.Add(numeroTader);
                    listaValidada = null;

                }

                



            }
            return (listaretorno);
        }

        public void Auditor(List<string> list_SemP, List<string> list_ComP)
        {
           
            var container = ODataContextWrapper.GetContainer();
            
          
            var nome = container.DomainValues.Where(x => x.DomainId == 39).ToList();
            var acc = container.DomainValues.Where(x => x.DomainId == 40).ToList();
            var req = container.DomainValues.Where(x => x.DomainId == 41).ToList();
            var agile = container.DomainValues.Where(x => x.DomainId == 42).ToList();
            //preenche a matriz de acordo com cada auditor
            P_campo_nome(nome,0);
            P_campo(acc,1);
            P_campo(req,2);
            P_campo(agile,3);
            CountPE(nome);

            #region Ordenação da matriz refletida
            double[,] matrizAs = new double[nome.Count, 5];
            double valor = 0;

            for (int i = 0; nome.Count > i; i++)
                if (valor < matrizAD[i, 4])
                    valor = matrizAD[i, 4];

                for (int i = 0; nome.Count > i; i++)
                   {
                matrizAs[i, 4] = valor;
                for (int j = 0; nome.Count > j; j++)
                    if (matrizAs[i, 4] > matrizAD[j, 4]&& matrizAD[j, 5]!= 1)
                    {
                        
                        matrizAs[i, 0] = matrizAD[j, 0];
                        matrizAs[i, 1] = matrizAD[j, 1];
                        matrizAs[i, 2] = matrizAD[j, 2];
                        matrizAs[i, 3] = matrizAD[j, 3];
                        matrizAs[i, 4] = matrizAD[j, 4];
                        
                    }

                for (int j = 0; nome.Count > j; j++)
                    if (matrizAs[i, 0] == matrizAD[j, 0])
                    {

                        matrizAD[j, 5] = 1;

                    }


            }
            #endregion

            #region Assignação
            foreach (string id in list_SemP)
            {
                var peticao = listaTader.Where(x => x.peticion.ToString() == id).First();
                Ticket obj = _robot.Tickets.Where(x => x.Description.ToString() == id).First();
                int linha = 0;
                //verifica o atual maior valor da lista para usalo como parametro
                for (int i = 0; nome.Count > i; i++)
                    if (valor < matrizAs[i, 4])
                        valor = matrizAs[i, 4];
                //chama a categoria
                int categoria = Convert.ToInt32(obj.TicketValues.Where(x => x.FieldId == 244));

                int coluna = 0;
                //indentifica a categoria da peticao
                if (categoria == 277)
                    coluna = 1;

                if (categoria == 279)
                    coluna = 2;

                if (categoria == 278)
                    coluna = 3;
                //valida o menor peso
                for (int i = 0; nome.Count > i; i++)
                {
                    if(matrizAs[i, 4] < valor && matrizAs[i, coluna] == 1)
                    {
                        valor = matrizAs[i, 4];
                        linha = i;

                    }
                }
                //valida o peso da petição e o atribui
                if (peticao.estado == "ENTREGADA")
                    matrizAs[linha, 4] = valor + 0.5;
                else
                    matrizAs[linha, 4] = valor + 1;

                //finalmente asigna
                if (matrizAs[linha, 1] == 1)
                    {
                        obj.TicketValues.Add(new TicketValue
                        {
                            CreationDate = DateTime.Now,
                            FieldId = 234,
                            ClonedValueOrder = null,
                            TicketId = obj.Id,
                            Value = matrizAs[linha,0].ToString()
                        });

                    }else
                    if (matrizAs[linha, 2] == 1)
                    {
                        obj.TicketValues.Add(new TicketValue
                        {
                            CreationDate = DateTime.Now,
                            FieldId = 234,
                            ClonedValueOrder = null,
                            TicketId = obj.Id,
                            Value = matrizAs[linha, 0].ToString()
                        });

                    }else
                    if (matrizAs[linha, 3] == 1)
                    {
                        obj.TicketValues.Add(new TicketValue
                        {
                            CreationDate = DateTime.Now,
                            FieldId = 234,
                            ClonedValueOrder = null,
                            TicketId = obj.Id,
                            Value = matrizAs[linha, 0].ToString()
                        });

                    }
                

            }
            #endregion
        }

        public void CountPE(List<DomainValue>nome)
        {
            var container = ODataContextWrapper.GetContainer();
            var status = container.DomainValues.Where(x => x.DomainId == 15).ToList();
            int idStatus = 0;
            foreach (var value in status)
            {
                if (value.Value == "ENTREGADA")
                    idStatus = value.Id;
            }
            double list = 0;
            for (int i = 0; nome.Count > i; i++)
            {
                


                foreach (Ticket ticket in _robot.Tickets)
                {
                    if (ticket.TicketValues.Where(o => o.FieldId == 234).ToString() == matrizAD[i, 0].ToString() && ticket.TicketValues.Where(o => o.FieldId == 231).ToString() == idStatus.ToString())
                    {
                        list = (list + 1) * 0.5;
                    }
                    else if (ticket.TicketValues.Where(o => o.FieldId == 234).ToString() == matrizAD[i, 0].ToString() && ticket.TicketValues.Where(o => o.FieldId == 231).ToString() != idStatus.ToString() )
                        list++;
                    


                }
                matrizAD[i, 4] = list;

            }
            
           


        }

        public void P_campo_nome(List<DomainValue> domino, int coluna)
        {
            int linha = 0;
            foreach (var value in domino)
            {
                matrizAD[linha, coluna] = value.Id;
                linha++;
            }
        }

        public void P_campo(List<DomainValue> domino, int coluna)
        {
            int linha = 0;
            foreach (var value in domino)
            {
                matrizAD[linha, coluna] = Convert.ToDouble(value.Value);
                linha++;
            }
        }



        public void UploadFile(string nomeArquivo, string destino, string localArquivo, Ticket ticket)
        { 

            nomeArquivo = nomeArquivo + ".xlsm";
            string pathDestino = System.IO.Path.Combine(destino, nomeArquivo);
            string pathOrigem = System.IO.Path.Combine(localArquivo, nomeArquivo);

            if (!System.IO.Directory.Exists(pathDestino))
            {
                System.IO.Directory.CreateDirectory(pathDestino);
            }

            System.IO.File.Copy(pathOrigem, pathDestino, true);

            ticket.TicketValues.Add(new TicketValue { ClonedValueOrder = null, TicketId = ticket.Id, FieldId = 245, Value = pathDestino });


        }







      
    }
    }


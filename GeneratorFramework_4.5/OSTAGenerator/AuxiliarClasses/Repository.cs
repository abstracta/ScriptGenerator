using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.SCLSnippets;
using Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses.Scripts;

namespace Abstracta.Generators.Framework.OSTAGenerator.AuxiliarClasses
{
    internal class Repository
    {
        /*   Respository:
              tiene un conjunto de DataFile, Script y Tests
         *    (por defecto ya crea un script que sea global_variables, funciones y variablesfunciones) 
           
         * 
         * NOTA: NO SE SI TIENE MUCHO SENTIDO TENER LA LISTA DE DATAFILES Y SCRIPTS YA QUE 
         * SI SE VAN A IR CREANDO DE A UNO, ES MEJOR INVOCAR DESDE FUERA DIRECTAMENTE AL 
         * CONSTRUCTOR DE ESTOS ELEMENTOS, PIDIÉNDOLES A ELLSO QUE SE "BAJEN A DISCO"
         * 
         */

        //TODO:  Test: este lo dejamos para mas adelante. Sería bueno crear un escenario basico con todos los scripts
        
        public static string GLOBAL_VARIABLES = "global_variables.inc";
        public static string FUNCTION_VARIABLES = "function_variables.inc";
        public static string FUNCTIONS = "functions.htp";

        private StreamWriter _fileGlobalVariables;
        private StreamWriter _fileFunctionVariables;
        private StreamWriter _fileFunctions;

        /* PUBLIC GETTERS AND SETTERS */

        public string Path { get; private set; }

        public string ScriptsPath { get; private set; }
        public string IncludePath { get; private set; }
        public string TestsPath { get; private set; }
        public string DataPath { get; private set; }

        private List<DataFile> Datafiles
        {
            get { return Datafiles; }
        }

        public Dictionary<string, IScript> Scripts { get; set; }
        public ScriptSCL Global { get; set; }
        public MainScriptSCL MainScl { get; set; }

        public Repository(string path, string mainScriptName)
        {
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            Path = path;
            ScriptsPath = Path + "Scripts";
            IncludePath = Path + "Scripts\\Include";
            TestsPath = Path + "Tests";
            DataPath = Path + "Data";
            Scripts = new Dictionary<string, IScript>();
            MainScl = CreateScript(mainScriptName, true) as MainScriptSCL;
            LoadDefaultScripts();
            RequestSection.ConnectionID = 1;
            RequestSection.StepCounter = 1;
            RequestSection.FirstRequest = true;
            RequestSection.PreviousRequestResponse = new Dictionary<string, int>();
            RequestSection.PreviousRedirectURL = "";
            RequestSection.Pwd = null;
            RequestSection.User = null;
        }

        private void LoadDefaultScripts()
        {
            Global = new ScriptInc("GLOBAL_VARIABLES.INC", "MR", this);
            Scripts.Add(IncludePath + "\\" + Global.Name, Global);
            LoadDefaultValuesToGlobal();

            var funciones = new IncludedScriptSCL("Functions.htp", "MR", this, MainScl);
            Scripts.Add(IncludePath + "\\" + funciones.Name, funciones);
            LoadFuntions(funciones);

            var responseCodes = new ScriptInc("response_codes.inc", "MR", this);
            Scripts.Add(IncludePath + "\\" + responseCodes.Name, responseCodes);
            LoadResponseCodes(responseCodes);

            var differenceValidation = new ScriptInc("DifferenceValidation.inc", "MR", this);
            Scripts.Add(IncludePath + "\\" + differenceValidation.Name, differenceValidation);
            LoadDifferenceValidation(differenceValidation);

            var equalValidation = new ScriptInc("EqualValidation.inc", "MR", this);
            Scripts.Add(IncludePath + "\\" + equalValidation.Name, equalValidation);
            LoadEqualValidation(equalValidation);

            var appearText = new ScriptInc("AppearText.inc", "MR", this);
            Scripts.Add(IncludePath + "\\" + appearText.Name, appearText);
            LoadAppearText(appearText);

            var notAppearText = new ScriptInc("NotAppearText.inc", "MR", this);
            Scripts.Add(IncludePath + "\\" + notAppearText.Name, notAppearText);
            LoadNotAppearText(notAppearText);

            var functionsVariables = new ScriptInc("FunctionsVariables.inc", "MR", this);
            Scripts.Add(IncludePath + "\\" + functionsVariables.Name, functionsVariables);
            LoadFunctionsVariables(functionsVariables);

        }

        private static void LoadEqualValidation(ScriptSCL equalValidation)
        {
            if (equalValidation == null) throw new ArgumentNullException("equalValidation");

            #region EqualValidation

            equalValidation.AddString(@"
	START TEST-CASE stepName 
		IF ( valorObtenido <> expectedResponse) THEN
	  		SET v_fail = 1
	 	 	FAIL TEST-CASE
		ELSE 
			SET v_fail = 0
	 	ENDIF
	END TEST-CASE
	
	IF (v_fail = 1) THEN
		SET MESSAGE = 'ERROR:: ' + valorObtenido + ' es distinto a '+ expectedResponse
		GOTO ERR_LABEL
	ENDIF
");

            #endregion
        }

        private static void LoadAppearText(ScriptSCL appearText)
        {
            if (appearText == null) throw new ArgumentNullException("appearText");

            #region AppearText

            appearText.AddString(@"
	START TEST-CASE stepName    
		Set v_fail = ~LOCATE( expectedResponse,buffer),CASE_BLIND
		IF ( v_fail =-1) THEN
	  		SET v_fail = 1			    
	 	 	FAIL TEST-CASE
		ELSE  
			SET v_fail = 0
	 	ENDIF
	END TEST-CASE
	
	IF (v_fail = 1) THEN
		SET MESSAGE = 'ERROR:: El texto '+ expectedResponse+' no se encuentra'
		Report 'El HTML Recibido es: ', buffer
		log 'El HTML Recibido es: ', buffer
		GOTO ERR_LABEL
	ENDIF                                                                                   
	");

            #endregion
        }

        private static void LoadNotAppearText(ScriptSCL notAppearText)
        {
            if (notAppearText == null) throw new ArgumentNullException("notAppearText");

            #region notAppearText

            notAppearText.AddString(@"
	START TEST-CASE stepName     
		Set v_fail =  ~LOCATE( expectedResponse,buffer),CASE_BLIND
		IF ( v_fail <>-1) THEN
	  		SET v_fail = 1
	 	 	FAIL TEST-CASE
		ELSE 
			SET v_fail = 0
	 	ENDIF
	END TEST-CASE
	
	IF (v_fail = 1) THEN		
		SET MESSAGE = 'ERROR:: El texto '+ expectedResponse+' no se encuentra'
		Report 'El HTML Recibido es: ', buffer
		log 'El HTML Recibido es: ', buffer		
		GOTO ERR_LABEL
	ENDIF


");

            #endregion
        }

        private static void LoadFunctionsVariables(ScriptSCL functionsVariables)
        {
            #region FunctionsVariables

            functionsVariables.AddString(@"
	![MR] 22-11-06 Este archivo contiene todas las variables utilizadas en las funciones          

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!variables para la subrutina ParseString

CHARACTER 		F_Separador	
CHARACTER*2048	F_Entrada	

Integer F_LargoEntrada
Integer F_Step !sirve para hacer el repeatUntil
Integer F_i1	!sirve para hacer el repeatUntil
Integer F_fin1	!indica la posicion final
Integer F_posicion !me indica la posicion en el arreglo de resultado que estoy escribiendo

CONSTANT F_LARGO_MAX = 50 ! especifica el largo maximo que puede tener cada string a parsear
CONSTANT F_CANT_MAX = 20  !especifica la cantidad maxima de strings a parsear
!salida
Integer 	cantParseado ! en esta variable se devuelve la cantidad de Strings parseados
CHARACTER:F_LARGO_MAX Resultado [0:F_CANT_MAX] !cada elemento del arreglo contiene un string parseado

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!variables para la subrutina FechaManiana

Integer 		dia
Integer 		mes
Integer 		anio
Character*50 	resp
Character*9999	aux
Integer 		iaux 
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

integer pos
integer largo
integer Offset                     
integer Offset2 
CHARACTER*10048  strAux


!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!variables para la subrutina Between
CHARACTER*256  strInicial
CHARACTER*256  strFinal
CHARACTER*1024 strABuscar
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

character*32  str1
character*32  str2
character*32  fecha
integer       num
integer       i
integer       itmp
integer       itmp2
integer       mes_largo
integer       anio_bis

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!variables para la subrutina getUrlEncriptada

CHARACTER*262143  F_javaScript		  
CHARACTER*256	F_nombreMenu
CHARACTER*256  F_urlEncriptada

integer F_pos
integer F_aux

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para getLastCookie

Character*2048 F_cabezal
Character*1024 F_nombreCookie
Character*1024 F_Cookie

Integer F_fin
Integer F_i
Integer F_largo
Integer F_intStep
Character*2048 F_anterior

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para getUrlParaPopUp y para URLENCODE

Character*512     F_SB_URL
Character*196605  F_llamadorPopup
Character*128     F_nombrePopup
Character*512     F_urlEncPopup
Integer           F_pos2
Integer           F_aux2
Integer           F_SB_INDEX
Integer           F_SB_INDEX2
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para la funcion de get timestamp
Character*128	timestamp
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para la funcion de getUrlMainForm
Character*512	urlMainForm
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
Character*256	urlMainAnterior

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para la funcion de getRedirect
Character*512	urlRedirect
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
                                                                          
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para la funcion de getCookies
CONSTANT 		MAX_CANT_COOKIES =  19
Character*32	NombresCookie[1:MAX_CANT_COOKIES]
Character*512	ValoresCookie[1:MAX_CANT_COOKIES]
Integer 		tope
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	
!variables para la seccion de followredirect
Character*256	cookie
Character*2048	cookies_perm
Character*256	valorCookie   
Character*4000	cookies                                              
Integer 		AuxLoop
Integer 		AuxLoop2
Character*60000 last_header
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!	

");

            #endregion
        }

        private static void LoadDifferenceValidation(ScriptSCL differenceValidation)
        {
            #region differencevalidation

            differenceValidation.AddString(@"	
	START TEST-CASE stepName 
		IF ( valorObtenido = expectedResponse) THEN
	  		SET v_fail = 1
	 	 	FAIL TEST-CASE
		ELSE 
			SET v_fail = 0
	 	ENDIF
	END TEST-CASE
	
	IF (v_fail = 1) THEN
		SET MESSAGE = 'ERROR:: Valor obtenido ' + valorObtenido + ' NO es distinto a '+ expectedResponse
		GOTO ERR_LABEL
	ENDIF


");

            #endregion
        }

        private static void LoadResponseCodes(ScriptSCL responseCodes)
        {
            #region responsescodes

            responseCodes.AddString(@"!/////////////////////////////////////////////////////////////////////////
!// file: response_codes.inc                                            //
!// This file is distributed as part of OpenSTA - http://opensta.org/   //
!// It is included in all generated scripts and contains HTTP response  //
!// codes and other constant definitions.                               //
!/////////////////////////////////////////////////////////////////////////
CONSTANT    CONTINUE = 100                  ! Continue
CONSTANT    SWITCH_PROTOCOLS = 101          ! Switching Protocols
CONSTANT    OK = 200                        ! OK
CONSTANT    CREATED = 201                   ! Created
CONSTANT    ACCEPTED = 202                  ! Accepted
CONSTANT    NON_AUTH_INFO = 203             ! Non-Authoritative Information
CONSTANT    NO_CONTENT = 204                ! No Content
CONSTANT    RESET_CONTENT = 205             ! Reset Content
CONSTANT    PARTIAL_CONTENT = 206           ! Partial Content
CONSTANT    MULTIPLE_CHOICES = 300          ! Multiple Choices
CONSTANT    MOVED = 301                     ! Moved Permanently
CONSTANT    FOUND = 302                     ! Found
CONSTANT    SEE_OTHER = 303                 ! See Other
CONSTANT    NOT_MODIFIED = 304              ! Not Modified
CONSTANT    USE_PROXY = 305                 ! Use Proxy
CONSTANT    TEMPORARY = 307                 ! Temporary Redirect
CONSTANT    BAD_REQ = 400                   ! Bad PrimaryRequest
CONSTANT    UNAUTHORIZED = 401              ! Unauthorized
CONSTANT    PAYMENT_REQ = 402               ! Payment Required
CONSTANT    FORBIDDEN = 403                 ! Forbidden
CONSTANT    NOT_FOUND = 404                 ! Not Found
CONSTANT    NOT_ALLOWED = 405               ! Method Not Allowed
CONSTANT    NOT_ACCEPTABLE = 406            ! Not Acceptable
CONSTANT    PROXY_AUTH_REQ = 407            ! Proxy Authentication Required
CONSTANT    REQ_TIMEOUT = 408               ! PrimaryRequest Time-out
CONSTANT    CONFLICT = 409                  ! Conflict
CONSTANT    GONE = 410                      ! Gone
CONSTANT    LENGTH_REQ = 411                ! Length Required
CONSTANT    PRECOND_FAILED = 412            ! Precondition Failed
CONSTANT    REQ_ENTITY_2BIG = 413           ! PrimaryRequest Entity Too Large
CONSTANT    REQ_URI_2BIG = 414              ! PrimaryRequest-URI Too Large
CONSTANT    UNSUP_MEDIA = 415               ! Unsupported Media Type	
CONSTANT    OUT_OF_RANGE = 416              ! Requested range not satisfied
CONSTANT    EXPECT_FAILED = 417             ! Expectation Failed
CONSTANT    SERVER_ERROR = 500              ! Internal Server Error
CONSTANT    NOT_IMPLEMENTED = 501           ! Not Implemented
CONSTANT    BAD_GATEWAY = 502               ! Bad Gateway
CONSTANT    NO_SERVICE = 503                ! Service Unavailable
CONSTANT    GATEWAY_TIMEOUT = 504           ! Gateway Time-out
CONSTANT    UNSUP_HTTP_VER = 505            ! HTTP Version not supported

CONSTANT    REQ_SUCCEEDED = 1               ! Successful statement
CONSTANT    REQ_HTTP_CODE = 0               ! HTTP request succeeded.
CONSTANT    REQ_GENERIC_ERR = -1            ! Generic Error
CONSTANT    REQ_SOCKET_ERR = -2             ! Socket Error
");

            #endregion
        }

        private static void LoadFuntions(ScriptSCL funciones)
        {
            #region funciones

            funciones.AddString(@"!Browser:IE5
![MR] 22-11-06 Este archivo contiene todas las funciones utilizadas en los scripts

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de maniana.
!
!Input: N/A                    
!
!Output: devuelve la fecha de maniana en formato dd%2Fmm%2faaaa
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaManiana
     call cargarFecha

     !log   '-',dia,'-',mes,'-',anio
     Convert dia To Resp
     Set Resp = Resp + '%2F' 
!     Convert mes To aux     
	 call MesANumero 
     Set Resp = Resp + Resultado[1] + '%2F' + Resultado[2]
!	 Convert anio To aux           
!  	 Set Resp = Resp + aux
END SUBROUTINE

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de maniana y la devuelve en otro formato
!
!Input: N/A                    
!
!Output: devuelve la fecha de maniana en formato aaaammdd
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaManiana2
     call cargarFecha

     !log   '-',dia,'-',mes,'-',anio
     Convert dia To Resp
!     Convert mes To aux     
	 call MesANumero            
	 
 	 if(dia <= 9) then
	      set Resp = '0' + Resp
	 endif
	 
     Set Resp =   Resultado[2] + Resultado[1] +  Resp 
!	 Convert anio To aux           
!  	 Set Resp = Resp + aux
END SUBROUTINE
      
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de maniana y la devuelve en otro formato
!
!Input: N/A                    
!
!Output: devuelve la fecha de maniana en formato dd%2Fmm%2Faa
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaManiana3
     call cargarFecha

     !log   '-',dia,'-',mes,'-',anio
     Convert dia To Resp
!     Convert mes To aux     
	 call MesANumero            
	 
 	 if(dia <= 9) then
	      set Resp = '0' + Resp
	 endif
	 
	 set Resultado[2] = ~LEFTSTR(2, Resultado[2]) 
     Set Resp =   Resp +'%2F'+ Resultado[1] +'%2F'+ Resultado[2] 
END SUBROUTINE
    
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de hoy y la devuelve en otro formato
!
!Input: N/A                    
!
!Output: devuelve la fecha de hoy en formato aaaa-mm-dd
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaHoy
     call cargarFecha2

     !log   '-',dia,'-',mes,'-',anio
!    Convert dia To Resp
!     Convert mes To aux     
	 call MesANumero 
     Set Resp =   Resultado[2] +'-'+ Resultado[1] + '-'+ Resultado[0]
!	 Convert anio To aux           
!  	 Set Resp = Resp + aux
END SUBROUTINE

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de hoy y la devuelve en otro formato
!
!Input: N/A                    
!
!Output: devuelve la fecha de hoy en formato dd%2Fmm%2Faa
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaHoy2
     call cargarFecha2

     !log   '-',dia,'-',mes,'-',anio
!     Convert dia To Resp

	 call MesANumero       
	 set Resultado[2] = ~RightSTR(2, Resultado[2]) 
     Set Resp =   Resultado[0] +'%2F'+ Resultado[1] +'%2F'+ Resultado[2] 
END SUBROUTINE  

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de hoy y la devuelve en otro formato
!
!Input: N/A                    
!
!Output: devuelve la fecha de hoy en formato dd%2Fmm%2Faaaa
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaHoy3
     call cargarFecha2

     !log   '-',dia,'-',mes,'-',anio
!     Convert dia To Resp

	 call MesANumero       
	 set Resultado[2] = ~RightSTR(2, Resultado[2]) 
     Set Resp =   Resultado[0] +'%2F'+ Resultado[1] +'%2F20'+ Resultado[2] 
END SUBROUTINE  

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene la fecha de hoy y la devuelve en otro formato
!
!Input: N/A                    
!
!Output: devuelve la fecha de hoy en formato aaaammdd
!CHARACTER  
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!  
SUBROUTINE FechaHoy4
     call cargarFecha2

     !log   '-',dia,'-',mes,'-',anio
!     Convert dia To Resp

	 call MesANumero       
	 set Resultado[2] = ~RightSTR(2, Resultado[2]) 
     Set Resp =   '20'+Resultado[2]+ Resultado[1] + Resultado[0]
END SUBROUTINE  


![MR] 7/12/06 - se agregan estas funciones ya que la funcion estandar del opensta conver, no
!funcionaba en algunas PCs
			SUBROUTINE MesMasUno          
			
				if ( Resultado[1] = 'JAN') THEN
					set Resultado[1] = 'FEB'
				ELSEIF (Resultado[1] = 'FEB') THEN
					set Resultado[1] = 'MAR'
				ELSEIF (Resultado[1] = 'MAR') THEN
					set Resultado[1] = 'APR'
				ELSEIF (Resultado[1] = 'APR') THEN
					set Resultado[1] = 'MAY'
				ELSEIF (Resultado[1] = 'MAY') THEN
					set Resultado[1] = 'JUN'
				ELSEIF (Resultado[1] = 'JUN') THEN
					set Resultado[1] = 'JUL'
				ELSEIF (Resultado[1] = 'JUL') THEN
					set Resultado[1] = 'AUG'
				ELSEIF (Resultado[1] = 'AUG') THEN
					set Resultado[1] = 'SEP'
				ELSEIF (Resultado[1] = 'SEP') THEN
					set Resultado[1] = 'OCT'
				ELSEIF (Resultado[1] = 'OCT') THEN
					set Resultado[1] = 'NOV'	
				ELSEIF (Resultado[1] = 'NOV') THEN
					set Resultado[1] = 'DEC'
				ELSEIF (Resultado[1] = 'DEC') THEN
					set Resultado[1] = 'JAN'
				ENDIF
			END SUBROUTINE
			
			SUBROUTINE MesANumero          
				if ( Resultado[1] = 'JAN') THEN
					set Resultado[1] = '01'
				ELSEIF (Resultado[1] = 'FEB') THEN
					set Resultado[1] = '02'
				ELSEIF (Resultado[1] = 'MAR') THEN
					set Resultado[1] = '03'
				ELSEIF (Resultado[1] = 'APR') THEN
					set Resultado[1] = '04'
				ELSEIF (Resultado[1] = 'MAY') THEN
					set Resultado[1] = '05'
				ELSEIF (Resultado[1] = 'JUN') THEN
					set Resultado[1] = '06'
				ELSEIF (Resultado[1] = 'JUL') THEN
					set Resultado[1] = '07'
				ELSEIF (Resultado[1] = 'AUG') THEN
					set Resultado[1] = '08'
				ELSEIF (Resultado[1] = 'SEP') THEN
					set Resultado[1] = '09'
				ELSEIF (Resultado[1] = 'OCT') THEN
					set Resultado[1] = '10'	
				ELSEIF (Resultado[1] = 'NOV') THEN
					set Resultado[1] = '11'
				ELSEIF (Resultado[1] = 'DEC') THEN
					set Resultado[1] = '12'
				ENDIF
			END SUBROUTINE

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Subrutina para parsear un string con formato 'variable1 | variable2 | ...'
!se pueden setear distintos separadores
!el separador debe ser un string de largo 1
!se pueden leer cualquier cantidad de variables
!sirve principalmente para leer valores relacionados en una sola leída
!
!Input:   
!CHARACTER 		F_Separador	
!CHARACTER*256	F_Entrada	
!
!Output:
!Integer 	cantParseado ! en esta variable se devuelve la cantidad de Strings parseados
!CHARACTER:F_LARGO_MAX Resultado [0:F_CANT_MAX] !cada elemento del arreglo contiene un string parseado
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
SUBROUTINE ParseString 
	SET F_Step = 0
	SET F_posicion = 0
	
	DO F_i1=0,1,F_Step
		SET F_Entrada = ~LTRIM(F_Entrada) !saco los espacios del principio
		SET F_fin1 = ~LOCATE(F_Separador, F_Entrada) 		
		IF (F_fin1=-1) THEN
			SET F_Step = 1 !termino el loop
			!copio el ultimo string
			SET Resultado[F_posicion] = F_Entrada
			
		ELSE
			SET Resultado[F_posicion] = ~EXTRACT(0, F_fin1, F_Entrada)			
			SET F_posicion = F_posicion + 1
			SET F_LargoEntrada = ~LENGTH(F_Entrada)
			set F_LargoEntrada = F_LargoEntrada - (F_fin1 + 1)
			SET F_Entrada = ~RIGHTSTR(F_LargoEntrada, F_Entrada) !elimino el pimer string y el caracter separador
			set F_LargoEntrada = F_LargoEntrada + F_fin1 + 1
		ENDIF
	ENDDO	
	RETURN
END SUBROUTINE
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
                      
!Esta es un rutina auxiliar que sera usada por otras funciones para cargar la fecha
SUBROUTINE cargarFecha
	LOAD DATE Into Resp        
	
	set F_Separador = '-'
	set F_Entrada = Resp
	call parseString
    
    
	if (Resultado[1] = 'JAN') THEN
		set mes = 1
	ELSEIF (Resultado[1] = 'FEB') THEN
		set mes = 2
	ELSEIF (Resultado[1] = 'MAR') THEN
		set mes = 3
	ELSEIF (Resultado[1] = 'APR') THEN
		set mes = 4
	ELSEIF (Resultado[1] = 'MAY') THEN
		set mes = 5
	ELSEIF (Resultado[1] = 'JUN') THEN
		set mes = 6
	ELSEIF (Resultado[1] = 'JUL') THEN
		set mes = 7
	ELSEIF (Resultado[1] = 'AGO') THEN
		set mes = 8
	ELSEIF (Resultado[1] = 'SEP') THEN
		set mes = 9
	ELSEIF (Resultado[1] = 'OCT') THEN
		set mes = 10	
	ELSEIF (Resultado[1] = 'NOV') THEN
		set mes = 11
	ELSEIF (Resultado[1] = 'DEC') THEN
		set mes = 12
	ENDIF
        

    Convert Resultado[2] to anio 
    Convert Resultado[0] to dia     
    
    Set dia = dia + 1
    
    if (dia = 29) AND (mes = 2) THEN
    	set mes = mes +1    
    	call MesMasUno
    	set dia = 1
    	Goto devolverFecha
    endif
    if (dia = 31) AND ((mes = 4) OR (mes = 6) OR (mes = 9) OR (mes =11)) THEN
    	set mes = mes +1 
    	call MesMasUno
    	set dia = 1
    	Goto devolverFecha
    endif    
    if (dia = 32) AND ((mes = 1) OR (mes = 3) OR (mes = 5) OR (mes = 7) OR (mes = 8) OR (mes = 10) OR (mes = 12)) THEN
    	set mes = mes +1    
    	call MesMasUno
    	set dia = 1     
    	if (mes = 1) then
    		set anio = anio +1
    		IF (anio = 2006) THEN 
    			SET Resultado[2] = '2007' 
    		ELSEIF (anio = 2007) THEN
    			SET Resultado[2] = '2008'   
    		ELSEIF (anio = 2008) THEN
    		    SET Resultado[2] = '2009' 
    		ELSEIF (anio = 2009) THEN 
    		    SET Resultado[2] = '2010' 
    		ELSEIF (anio = 2010) THEN 
    		    SET Resultado[2] = '2011' 
    		endif
    	endif
    	Goto devolverFecha
    endif       
 devolverFecha:
end subroutine

SUBROUTINE cargarFecha2
	LOAD DATE Into Resp  
	set F_Separador = '-'
	set F_Entrada = Resp
	call parseString
    
    
	if (Resultado[1] = 'JAN') THEN
		set mes = 1
	ELSEIF (Resultado[1] = 'FEB') THEN
		set mes = 2
	ELSEIF (Resultado[1] = 'MAR') THEN
		set mes = 3
	ELSEIF (Resultado[1] = 'APR') THEN
		set mes = 4
	ELSEIF (Resultado[1] = 'MAY') THEN
		set mes = 5
	ELSEIF (Resultado[1] = 'JUN') THEN
		set mes = 6
	ELSEIF (Resultado[1] = 'JUL') THEN
		set mes = 7
	ELSEIF (Resultado[1] = 'AGO') THEN
		set mes = 8
	ELSEIF (Resultado[1] = 'SEP') THEN
		set mes = 9
	ELSEIF (Resultado[1] = 'OCT') THEN
		set mes = 10	
	ELSEIF (Resultado[1] = 'NOV') THEN
		set mes = 11
	ELSEIF (Resultado[1] = 'DEC') THEN
		set mes = 12
	ENDIF
        

    !Convert Resultado[2] to anio 
    !Convert Resultado[0] to dia     
    
end subroutine



!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Obtiene el valor de la primer ocurrencia del atributo dado en la variable strAux
![NP] 22/01/2014 - Cambio la variable de retorno a strAux para poder encadenar llamadas
!a la funcion en el orden en que aparecen los parametros sin tener que recargar el
!buffer, y arreglo un bug en el indice pos.
!               
!Input: respuesta del pedido de la pagina y nombre de la variable buscada
!Character*65535 buffer  -respuesta de donde se desea sacar la informacion
!Character*1024 strAux
!
!Output: strAux - valor del parametro buscado
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
SUBROUTINE getFirstOc
    !          'PEDIDTRR' value='81'           en este caso se setea strAux = 'PEDIDTRR'
    set  strAux = '" + "\"" + @"' + strAux + '" + "\"" + @"'
    SET pos = ~LOCATE(strAux, buffer)   
    if (pos=-1) then
    	set strAux = 'ERROR'   
    	RETURN
    endif
    
    SET largo = ~LENGTH(buffer) - pos 
    SET buffer = ~RIGHTSTR(largo, buffer)   
    
    SET pos = ~LOCATE('value=" + "\"" + @"', buffer)+  7 ! los 7 son del 'value=" + "\"" + @"'
    if (pos=6) then
    	set strAux = 'ERROR'   
    	RETURN
    endif                            
    
    SET largo = ~LENGTH(buffer) - pos 
    SET buffer = ~RIGHTSTR(largo, buffer)
    
    SET offset = ~LOCATE('" + "\"" + @"', buffer) 
    if (offset=-1) then
    	set strAux = 'ERROR'   
    	RETURN
    endif
    SET strAux = ~EXTRACT(0,  offset , buffer) !obtengo el valor hasta la primera comilla       
    RETURN
END SUBROUTINE            

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Toma un string y sustituye los caracteres para codificarlo
!
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 SUBROUTINE ENCODESTR

 	C0:
 	Set Offset = ~LOCATE('&lt;', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 4
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%3C' &
 	       + ~EXTRACT(Offset2,131070,strAux)
        goto C0
 	ENDIF

 	C1:
 	Set Offset = ~LOCATE('&quot;', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 6
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%22' &
 	       + ~EXTRACT(Offset2,131070,strAux)
        goto C1
 	ENDIF
 
 	C2:
 	Set Offset = ~LOCATE('&gt;', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 4
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%3E' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto C2
 	ENDIF
 
  
   	CONTINUE1:
 	Set Offset = ~LOCATE('{', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%7B' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE1
 	ENDIF

 	CONTINUE2:
 	Set Offset = ~LOCATE('}',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%7D' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE2
 	ENDIF

 	CONTINUE3:
 	Set Offset = ~LOCATE('|',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%7C' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE2
 	ENDIF

 	CONTINUE4:
 	Set Offset = ~LOCATE(':',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%3A' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE4
 	ENDIF

 	CONTINUE5:
 	Set Offset = ~LOCATE('/',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%2F' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE5
 	ENDIF

 	CONTINUE6:
 	Set Offset = ~LOCATE('+',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%2B' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE6
 	ENDIF

 	CONTINUE7:
 	Set Offset = ~LOCATE('=',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%3D' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE7
 	ENDIF

 	CONTINUE8:
 	Set Offset = ~LOCATE(' ',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '+' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE8
 	ENDIF
  	
 	CONTINUE9:
 	Set Offset = ~LOCATE(',',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%2C' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE9
 	ENDIF
 	
 	![ab] 25/06/07-- Added ';' to URL ENCODE 
 	CONTINUE10:
 	Set Offset = ~LOCATE(';',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%3B' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE10
 	ENDIF
 	
 	![ab] 25/06/07-- Added '	' (tab) to URL ENCODE  
 	CONTINUE11:
 	Set Offset = ~LOCATE('^I',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%09' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE11
 	ENDIF

 	![ab] 25/06/07-- Added '\n' (linefeed o salto de linea) to URL ENCODE  
 	CONTINUE12:
 	Set Offset = ~LOCATE('^J',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%0D%0A' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE12
 	ENDIF
	
	CONTINUE13:
 	Set Offset = ~LOCATE('#',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%23' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE13
 	ENDIF
 	
 	
 	
 	CONTINUE18:
 	Set Offset = ~LOCATE('?',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%3F' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE18
 	ENDIF
 	
 	CONTINUE19:
 	Set Offset = ~LOCATE('!',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%21' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE19
 	ENDIF
 	
 	CONTINUE20:
	Set Offset = ~LOCATE('" + "\"" + @"',strAux)
	IF (Offset<>-1) THEN
		Set Offset2 = Offset + 1
		Set strAux = ~EXTRACT(0,Offset,strAux) + '%22' &
			+ ~EXTRACT(Offset2,131070,strAux)
		goto CONTINUE20
	ENDIF

 	
 	!CONTINUE21:
 	!Set Offset = ~LOCATE('ó',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%B3' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE21
 	!ENDIF
 	     
 	!CONTINUE22:
 	!Set Offset = ~LOCATE('Á',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%81' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE22
 	!ENDIF
 	
 	!CONTINUE23:
 	!Set Offset = ~LOCATE('Á',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%81' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE23
 	!ENDIF

 	!CONTINUE24:
 	!Set Offset = ~LOCATE('Ó',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%93' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE24
 	!ENDIF

 	CONTINUE25:
 	Set Offset = ~LOCATE('&',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%26' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE25
 	ENDIF

 	!CONTINUE26:
 	!Set Offset = ~LOCATE('´',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C2%B4' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE26
 	!ENDIF

 	!CONTINUE27:
 	!Set Offset = ~LOCATE('Á',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%81' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE27
 	!ENDIF

 	!CONTINUE28:
 	!Set Offset = ~LOCATE('Í',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%8D' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE28
 	!ENDIF

 	!CONTINUE29:
 	!Set Offset = ~LOCATE('Á',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%81' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE29
 	!ENDIF

 	CONTINUE30:
 	Set Offset = ~LOCATE('~<0xC9>',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 1
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%89' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE30
 	ENDIF
 	      
 	!CONTINUE31:
 	!Set Offset = ~LOCATE('Ñ',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C3%91' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE31
 	!ENDIF              
	
	!CONTINUE32:
 	!Set Offset = ~LOCATE('º',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + '%C2%BA' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE32
 	!ENDIF              
 	                
 END SUBROUTINE
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                



!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Toma un string y sustituye los caracteres para decodificarlo
!    cambia por ejemplo %3B por un punto y coma ;      
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 SUBROUTINE DECODE_STR
 
 	C0:
 	Set Offset = ~LOCATE('%3C', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '&lt;' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto C0
 	ENDIF

 	C1:
 	Set Offset = ~LOCATE('%22', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '&quot;' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto C1
 	ENDIF
 
 	C2:
 	Set Offset = ~LOCATE('%3E', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '&gt;' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto C2
 	ENDIF
 
  
   	CONTINUE1:
 	Set Offset = ~LOCATE('%7B', strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '{' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE1
 	ENDIF

 	CONTINUE2:
 	Set Offset = ~LOCATE('%7D',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '}' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE2
 	ENDIF

 	CONTINUE3:
 	Set Offset = ~LOCATE('%7C',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '|'&
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE2
 	ENDIF

 	CONTINUE4:
 	Set Offset = ~LOCATE('%3A',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + ':' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE4
 	ENDIF

 	CONTINUE5:
 	Set Offset = ~LOCATE('%2F',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '/' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE5
 	ENDIF

 	CONTINUE6:
 	Set Offset = ~LOCATE('%2B',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '+' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE6
 	ENDIF

 	CONTINUE7:
 	Set Offset = ~LOCATE('%3D',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '=' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE7
 	ENDIF
   
   !el símbolo de + no lo debo reemplazar
 	!CONTINUE8:
 	!Set Offset = ~LOCATE('+',strAux)
 	!IF (Offset<>-1) THEN
   	!   Set Offset2 = Offset + 1
 	!   Set strAux = ~EXTRACT(0,Offset,strAux) + ' ' &
 	!       + ~EXTRACT(Offset2,131070,strAux)
    !   goto CONTINUE8
 	!ENDIF
  	
 	CONTINUE9:
 	Set Offset = ~LOCATE('%2C',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + ',' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE9
 	ENDIF
 	
 	CONTINUE10:
 	Set Offset = ~LOCATE('%3B',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + ';' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE10
 	ENDIF
 	
 	CONTINUE11:
 	Set Offset = ~LOCATE('%09',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '^I' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE11
 	ENDIF

 	CONTINUE12:
 	Set Offset = ~LOCATE('%0D%0A',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 6
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '^J' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE12
 	ENDIF
	
	CONTINUE13:
 	Set Offset = ~LOCATE('%23',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '#' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE13
 	ENDIF
 	
 	
 
 	
 	CONTINUE18:
 	Set Offset = ~LOCATE('%3F',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '?' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE18
 	ENDIF
 	
 	CONTINUE19:
 	Set Offset = ~LOCATE('%21',strAux)
 	IF (Offset<>-1) THEN
   	   Set Offset2 = Offset + 3
 	   Set strAux = ~EXTRACT(0,Offset,strAux) + '!' &
 	       + ~EXTRACT(Offset2,131070,strAux)
       goto CONTINUE19
 	ENDIF
 	
 END SUBROUTINE
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

SUBROUTINE    NroAStr
    If (offset=0) THEN
        Set straux = '0'
    ELSEIF (offset=1) THEN
        Set straux = '1'
    ELSEIF (offset=2) THEN
        Set straux = '2'
    ELSEIF (offset=3) THEN
        Set straux = '3'
    ELSEIF (offset=4) THEN
        Set straux = '4'
    ELSEIF (offset=5) THEN
        Set straux = '5'
    ELSEIF (offset=6) THEN
        Set straux = '6'
    ELSEIF (offset=7) THEN
        Set straux = '7'
    ELSEIF (offset=8) THEN
        Set straux = '8'
    ELSEIF (offset=9) THEN
        Set straux = '9'
    ELSEIF (offset=10) THEN
        Set straux = '10'
    ELSEIF (offset=11) THEN
        Set straux = '11'
    ELSEIF (offset=12) THEN
        Set straux = '12'
    ELSEIF (offset=13) THEN
        Set straux = '13'
    ELSEIF (offset=14) THEN
        Set straux = '14'
    ELSEIF (offset=15) THEN
        Set straux = '15'
    ELSEIF (offset=16) THEN
        Set straux = '16'
    ELSEIF (offset=17) THEN
        Set straux = '17'
    ELSEIF (offset=18) THEN
        Set straux = '18'
    ELSEIF (offset=19) THEN
        Set straux = '19'
    ELSEIF (offset=20) THEN
        Set straux = '20'
    ELSEIF (offset=21) THEN
        Set straux = '21'
    ELSEIF (offset=22) THEN
        Set straux = '22'
    ELSEIF (offset=23) THEN
        Set straux = '23'
    ELSEIF (offset=24) THEN
        Set straux = '24'
    ELSEIF (offset=25) THEN
        Set straux = '25'
    ELSEIF (offset=26) THEN
        Set straux = '26'
    ELSEIF (offset=27) THEN
        Set straux = '27'
    ELSEIF (offset=28) THEN
        Set straux = '28'
    ELSEIF (offset=29) THEN
        Set straux = '29'
    ELSEIF (offset=30) THEN
        Set straux = '30'
    ELSEIF (offset=31) THEN
        Set straux = '31'
    ELSEIF (offset=32) THEN
        Set straux = '32'
    ELSEIF (offset=33) THEN
        Set straux = '33'
    ENDIF
END SUBROUTINE


SUBROUTINE StrANro
 If (strAux='0') THEN
  Set iaux = 0
 ELSEIF (strAux='1') THEN
  Set iaux = 1
 ELSEIF (strAux='2') THEN
  Set iaux = 2
 ELSEIF (strAux='3') THEN
  Set iaux = 3
 ELSEIF (strAux='4') THEN
  Set iaux = 4
 ELSEIF (strAux='5') THEN
  Set iaux = 5
 ELSEIF (strAux='6') THEN
  Set iaux = 6
 ELSEIF (strAux='7') THEN
  Set iaux = 7
 ELSEIF (strAux='8') THEN
  Set iaux = 8
 ELSEIF (strAux='9') THEN
  Set iaux = 9
 ELSEIF (strAux='10') THEN
  Set iaux = 10
 ELSEIF (strAux='11') THEN
  Set iaux = 11
 ELSEIF (strAux='12') THEN
  Set iaux = 12
 ELSEIF (strAux='13') THEN
  Set iaux = 13
 ELSEIF (strAux='14') THEN
  Set iaux = 14
 ELSEIF (strAux='15') THEN
  Set iaux = 15
 ELSEIF (strAux='16') THEN
  Set iaux = 16
 ELSEIF (strAux='17') THEN
  Set iaux = 17
 ELSEIF (strAux='18') THEN
  Set iaux = 18
 ELSEIF (strAux='19') THEN
  Set iaux = 19
 ELSEIF (strAux='20') THEN
  Set iaux = 20
 ELSEIF (strAux='21') THEN
  Set iaux = 21
 ELSEIF (strAux='22') THEN
  Set iaux = 22
 ELSEIF (strAux='23') THEN
  Set iaux = 23
 ELSEIF (strAux='24') THEN
  Set iaux = 24
 ELSEIF (strAux='25') THEN
  Set iaux = 25
 ELSEIF (strAux='26') THEN
  Set iaux = 26
 ELSEIF (strAux='27') THEN
  Set iaux = 27
 ELSEIF (strAux='28') THEN
  Set iaux = 28
 ELSEIF (strAux='29') THEN
  Set iaux = 29
 ELSEIF (strAux='30') THEN
  Set iaux = 30
 ELSEIF (strAux='31') THEN
  Set iaux = 31
 ELSEIF (strAux='32') THEN
  Set iaux = 32
 ELSEIF (strAux='33') THEN
  Set iaux = 33
 ENDIF
END SUBROUTINE

!Dado el string strABuscar devuelve en straux el string que se 
!encuentra entre los substrings strInicial y strFinal
![NP] 22/01/2014 - Se agrega control de errores como en getFirstOc.
SUBROUTINE Between
	Set offset = ~LOCATE(strInicial, buffer)
    if (offset=-1) then
    	set strAux = 'ERROR'   
    	RETURN
    endif

	set offset  = ~length(buffer) - (offset + ~LENGTH(strInicial))
	set buffer = ~RIGHTSTR(offset, buffer)
	
	Set offset = ~LOCATE(strFinal, buffer)
    if (offset=-1) then
    	set strAux = 'ERROR'   
    	RETURN
    endif
	
	Set Straux = ~EXTRACT(0, offset, buffer)
end subroutine

! -----------------------------------------------------------
! obtiene una fecha y un nro de dias y calcula la nueva fecha
! formato fecha: DD/MM/AAAA
! retorno el resultado como String en STRAUX
! -----------------------------------------------------------
SUBROUTINE    sumaDias        

  set F_Separador = '/'
  set F_Entrada = fecha
  call ParseString 

  ! obtengo los dias como entero
  set strAux = Resultado[0]
  call StrANro
  set dia = iaux

  ! obtengo el mes como entero
  set strAux = Resultado[1]
  call StrANro
  set mes = iaux

  ! obtengo el año
  set str1 = Resultado[2]
  set str2 = ~EXTRACT(0,2,str1)
  set strAux = str2
  call StrANro
  set anio = iaux
  set str2 = ~EXTRACT(2,1,str1)
  set strAux = str2
  call StrANro
  set anio = (anio * 10) + iaux
  set str2 = ~EXTRACT(3,1,str1)
  set strAux = str2
  call StrANro
  set anio = (anio * 10) + iaux
             
! resto años
restoAnios:
  if (num<=365) then
      goto sig
  endif
 
  set num = num - 365
  set anio = anio + 1
  goto restoAnios
 
sig:      
  do i=1, num
    if ((mes = 1) or (mes = 3) or (mes = 5) or (mes = 7) or (mes = 8) or (mes = 10) or (mes = 12)) then
      set mes_largo = 1
    else
      set mes_largo = 0
    endif
   
    set itmp = anio % 4
    set itmp2 = anio % 400
    if ((itmp = 0) OR (itmp2 = 0)) then
      set anio_bis = 1
    else
      set anio_bis = 0
    endif
   
    set dia = dia + 1
    if (((dia = 31) AND (mes_largo = 0)) OR (dia = 32)) then
      set dia = 1
      set mes = mes + 1
    endif
   
    if ((mes = 2) AND (dia = 29) and (anio_bis = 0)) then
      set dia = 1
      set mes = 3
    elseif ((mes = 2) AND (dia = 30)) then
      set dia = 1
      set mes = 3
    endif
   
    if (mes = 13) then
      set mes = 1
      set anio = anio + 1
    endif
  enddo

  ! paso a string de nuevo
  set offset = dia
  call NroAStr
  if (dia < 10) then
    set strAux = '0'+strAux
  endif
  set buffer = strAux + '/'

  set offset = mes
  call NroAStr
  if (mes < 10) then
    set strAux = '0'+strAux
  endif
 
  set buffer = buffer + strAux + '/'

  set itmp2 = anio
  set itmp = 1000
  do i=1 ,4
    set itmp = anio / itmp2
    set itmp = itmp % 10
    set offset = itmp
    call NroAStr
   
    set buffer = buffer + strAux   
   
    set itmp2 = itmp2 / 10
  enddo

  ! RETORNO EL RESULTADO EN STRAUX
  set strAux = buffer
 
END SUBROUTINE
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
SUBROUTINE setTimestamp
	LOAD Time Into timestamp
end SUBROUTINE 
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!recibe en buffer el html y devuelve en urlMainForm la url del main form
SUBROUTINE getUrlMainForm
	set F_nombreMenu = 'form id=" + "\"" + @"MAINFORM" + "\"" + @" name=" + "\"" + @"MAINFORM" + "\"" + @" method=" + "\"" +
                                @"POST" + "\"" + @" ACTION=" + "\"" + @"'
	set F_pos = ~locate(F_nombreMenu, buffer)
	if (F_pos<>-1) then
		set F_aux = ~length(buffer) - (F_pos + ~length(F_nombreMenu) )
		set buffer = ~rightstr(F_aux, buffer)
					
		!esto es un ejemplo de lo que buscamos:
		
		
		set F_pos = ~locate('" + "\"" + @"', buffer) !busco el fin del parámetro para obtener el largo			
		!lo que busco está entre el '?' y la ','
		set urlMainForm = ~extract(0, F_pos, buffer)
	else
		Log 'ERROR: No se puede obtener la urlMainForm'
	endif					
	RETURN                                                                                	
end SUBROUTINE 




!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!Codifica una url para que los caracteres especiales desaparezcan
!Esto es necesario ya que en la aplicación hay un javascript q hace un 
!pedido codificando así una url dada     
!
!Esta subrutina la obtuvimos de la página del OpenSTA. Es una solución
!a un problema que hay con el VIEWSTATE de los sistemas hechos con .Net
!
!Input:                     
!CHARACTER*256 F_SB_URL - url sin codificar
!
!Output:
!CHARACTER*256  F_SB_URL - url codificada
!
!Fecha: 10-05-2006

!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 !SUBROUTINE URLENCODE [ F_SB_URL ]
 SUBROUTINE URLENCODE 
 
   	CONTINUE1:
 	Set F_SB_INDEX = ~LOCATE('{', F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%7B' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE1
 	ENDIF

 	CONTINUE2:
 	Set F_SB_INDEX = ~LOCATE('}',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%7D' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE2
 	ENDIF

 	CONTINUE3:
 	Set F_SB_INDEX = ~LOCATE('|',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%7C' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE2
 	ENDIF

 	CONTINUE4:
 	Set F_SB_INDEX = ~LOCATE(':',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%3A' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE4
 	ENDIF

 	CONTINUE5:
 	Set F_SB_INDEX = ~LOCATE('/',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%2f' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE5
 	ENDIF

 	CONTINUE6:
 	Set F_SB_INDEX = ~LOCATE('+',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%2B' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE6
 	ENDIF

 	CONTINUE7:
 	Set F_SB_INDEX = ~LOCATE('=',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%3D' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE7
 	ENDIF

      !-- Added 'white space' to URL ENCODE --Antony Marcano (remove this comment)
 	CONTINUE8:
 	Set F_SB_INDEX = ~LOCATE(' ',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%20' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE8
 	ENDIF
  	
    ![MR] 17/05/06-- Added ',' to URL ENCODE 
 	CONTINUE9:
 	Set F_SB_INDEX = ~LOCATE(',',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%2C' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE9
 	ENDIF
 	
 	![ab] 25/06/07-- Added ';' to URL ENCODE 
 	CONTINUE10:
 	Set F_SB_INDEX = ~LOCATE(';',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%3B' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE10
 	ENDIF
 	
 	![ab] 25/06/07-- Added '	' (tab) to URL ENCODE  
 	CONTINUE11:
 	Set F_SB_INDEX = ~LOCATE('^I',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%09' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE11
 	ENDIF

 	![ab] 25/06/07-- Added '\n' (linefeed o salto de linea) to URL ENCODE  
 	CONTINUE12:
 	Set F_SB_INDEX = ~LOCATE('^J',F_SB_URL)
 	IF (F_SB_INDEX<>-1) THEN
   	   Set F_SB_INDEX2 = F_SB_INDEX + 1
 	   Set F_SB_URL = ~EXTRACT(0,F_SB_INDEX,F_SB_URL) + '%0D%0A' &
 	       + ~EXTRACT(F_SB_INDEX2,65500,F_SB_URL)
       goto CONTINUE12
 	ENDIF

 END SUBROUTINE
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


");

            #endregion

        }

        private void LoadDefaultValuesToGlobal()
        {
            //TODO la variable DEFAULT_HEADERS se define en el GLOBAL_VARIABLES
            //no ponerle el gzip

            #region global

            Global.AddString(@"!/////////////////////////////////////////////////////////////////////////
!// file: global_variables.inc                                          //
!// This file is distributed as part of OpenSTA - http://opensta.org/   //
!// It is included in all generated scripts and is intended to contain  //
!// user defined variables and constants required at a global scope.    //
!/////////////////////////////////////////////////////////////////////////
                     
                     
!----------------------      
!para debug y manejo de errores
CONSTANT debug = '1'
CHARACTER*250000 buffer  
CHARACTER*250000 buffer2     
integer			responseCode  
CHARACTER*1024  strHeader       

integer v_fail, thread                                                           

integer codigo_http      
Integer			hacerLogin (1 - 1000)
Character*64	scriptName

!--------------------------------
![RV] Thinks times
!--------------------------------
CONSTANT TT_ALTO = 15000
CONSTANT TT_MEDIO = 10000
CONSTANT TT_CORTO = 2000     
                       
!----------------------
!variables de validaciones       
CHARACTER*256	stepName
CHARACTER*256	expectedResponse     
                     
Integer length 
Integer IdConexion  
                    

                          
 !-----------
!timer
Timer			T_Pag_Inicial
");

            #endregion
        }

        public void GenerateBasics()
        {
            /* genera el contenido básico del Repositorio en disco, en la ruta indicada. 

               crea carpeta para el repositorio dada en el constructor y dentro crea Data, Tests, Scripts 
               y dentro de esta últimacrea una llamada Include

               Crea dentro de /Scripts/Include tres archivos: 
                            * global_variables.inc
                            * function_variables.inc
                            * functions.inc
             
             */

            if (!Directory.Exists(Path))
            {
                //crear la carpeta Repository si no existe
                Directory.CreateDirectory(Path);
            }
            if (!Directory.Exists(DataPath))
            {
                //crear la carpeta Data si no existe
                Directory.CreateDirectory(DataPath);
            }
            if (!Directory.Exists(TestsPath))
            {
                //crear la carpeta Tests si no existe
                Directory.CreateDirectory(TestsPath);
            }
            if (!Directory.Exists(ScriptsPath))
            {
                //crear la carpeta Scripts si no existe
                Directory.CreateDirectory(ScriptsPath);
            }
            if (!Directory.Exists(IncludePath))
            {
                //crear la carpeta Include si no existe
                Directory.CreateDirectory(IncludePath);
            }

            //fileGlobalVariables = new StreamWriter(IncludePath + GLOBAL_VARIABLES);
            //fileFunctionVariables = new StreamWriter(IncludePath + FUNCTION_VARIABLES);
            //fileFunctions = new StreamWriter(IncludePath + FUNCTIONS);

            //StreamReader reader = new StreamReader(GLOBAL_VARIABLES);
            //string line = null;
            //while ((line = reader.ReadLine()) != null)
            //{
            //    fileGlobalVariables.WriteLine(line);
            //}
            //fileGlobalVariables.Close();
            //fileFunctionVariables.Close();
            //fileFunctions.Close();
        }

        // funcion auxilar que remueve los tildes del nombre de un script
        private static string RemoveAccentsFileName(string name)
        {
            var formD = name.Normalize(NormalizationForm.FormD);
            // la normalizacion a FormD divide a las letras con tilde en tilde+letra
            var sb = new StringBuilder();

            foreach (var c in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            // el resto remueve esos tildes (y otas non-spacing marks)
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
        
        internal ScriptSCL CreateScript(string scriptName, bool isMain)
        {
            var name = System.IO.Path.GetFileName(scriptName);
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, ' ');
            }

            // se remueven los tildes
            name = RemoveAccentsFileName(name);
            scriptName = name.Replace(" ", "");
            if (!scriptName.ToLower().EndsWith("htp"))
            {
                scriptName += ".htp";
            }

            ScriptSCL newScl;
            if (isMain)
            {
                MainScl = new MainScriptSCL(scriptName, "Generated from GXtest", "MR", this);
                newScl = MainScl;
                newScl.Path = ScriptsPath + @"\" + newScl.Name;
            }
            else
            {
                newScl = new IncludedScriptSCL(scriptName, "MR", this, MainScl);
                newScl.Path = IncludePath + @"\" + newScl.Name;
            }

            Scripts.Add(newScl.Path, newScl);
            return newScl;
        }

        internal void Write()
        {
            GenerateBasics();

            foreach (var sc in Scripts)
            {
                var scriptFolder = Directory.GetParent(sc.Key).FullName;
                sc.Value.Save(scriptFolder);
            }
        }
    }
}



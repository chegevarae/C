// MySQL in ZP connector
// Подготовка ZP. Идем в директорию куда установлен ZP и в папке Progs находим файл MySql.Data.dll
// Копируем его в папку ExternalAssemblies находящуюся там же.

// Добавляем в using на вкладку Общий код сразу после using ZennoLab.Emulation директиву:
using MySql.Data.MySqlClient;

// Далее, ниже в блок namespace ZennoLab.OwnCode следующий класс:

namespace ZennoLab.OwnCode
{
	/// <summary>
	/// A simple class of the common code
	/// </summary>
	public class CommonCode
	{
		/// <summary>
		/// Lock this object to mark part of code for single thread execution
		/// </summary>
		public static object SyncObject = new object();

		// Insert your code here
	}
	
	
	public class DB
	{
		private string hostname;
		private string username;
		private string password;
		private string database;
		private string charset;
		private string result;
		private MySqlConnection conn;
		
		
		public DB(string db_hostname, string db_username, string db_password, string db_database, string db_charset="utf8"){
			hostname = db_hostname;
			username = db_username;
			password = db_password;
			database = db_database;
			charset = db_charset;
			result = String.Empty;
			string db_port =  "3306";
			
			var m = db_hostname.Split(':');
			if ( m.Length == 2 ){
				db_hostname = m[0];
				db_port = m[1];
			}
			
			var connectionString = "server="+db_hostname+";user="+db_username+";database="+db_database+";port="+db_port+";password="+db_password+";pooling=False;";
			conn = new MySqlConnection(connectionString);

			open();
		}

		
		public void open(){
			conn.Open();
		}		

		
		public void close(){
			conn.Close();	
		}		
		
		public void query(string query){
			MySqlCommand command = new MySqlCommand(query, conn);
			command.ExecuteNonQuery();
		}
		
		public List<string> getAll(string query, string fieldSeparator="|"){
			
			var result = new List<string>();

            MySqlCommand command = new MySqlCommand(query, conn);			
			
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
				List<string> fields = new List<string>();
				for(int i=0; i<reader.FieldCount; i++)
				fields.Add(reader[i].ToString());
				
				result.Add(string.Join(fieldSeparator,fields));
				
            }
            reader.Close();
			
			return result;			
		}

		public List<string> getRow(string query){

            MySqlCommand command = new MySqlCommand(query, conn);			
			
            MySqlDataReader reader = command.ExecuteReader();
			
            if ( reader.Read() ){
         
				List<string> result = new List<string>();
				for(int i=0; i<reader.FieldCount; i++)
				result.Add(reader[i].ToString());

	            reader.Close();
				return result;				
            }
			
			reader.Close();
			return new List<string>();			
		}
		
		
		public string getOne(string query){
 			MySqlCommand command = new MySqlCommand(query, conn);
            string result = "";
			try { result = command.ExecuteScalar().ToString(); } catch{}
			return result;
		}
		
		
		public string escapeString(string text){			
			return MySql.Data.MySqlClient.MySqlHelper.EscapeString(text);
		}
		
	}	
	
}

// -------------------

string db_host = "localhost";     // хост
string db_user = "root";          // username для подключения к MySQL
string db_pswd = "";              // пароль для подключения к MySQL
string db_database = "mydb";      // название БД с которой будет работа
string db_charset = "utf8";       // кодировка данных в таблицах

// коннект к MySQL и открытие сессии

DB db = new DB(db_host, db_user, db_pswd, db_database, db_charset);

// все что идет ниже выполняется в рамках одного коннекта/сессии ... это очень важно (!)


// получить 1 результат (скаляр)
// для получения 1 результата всегда используем метод getOne

string count = db.getOne("SELECT COUNT(*) FROM accounts WHERE status=0");
project.SendInfoToLog("Кол-во аккаунтов: "+count,true); // выводим в лог ZP

// получить 1 запись ( запись = 1 строка разделенная на столбцы )
// для получения 1 записи/строки всегда используем метод getRow

List<string> row = db.getRow("SELECT first_name, last_name, status FROM accounts WHERE id=1");

if ( row.Count > 0 ){
    project.SendInfoToLog("Имя: "+row[0],true); // выводим в лог ZP
    project.SendInfoToLog("Фамилия: "+row[1],true);  // выводим в лог ZP
    project.SendInfoToLog("Статус: "+row[2],true);  // выводим в лог ZP
}
else {
    project.SendInfoToLog("запись отсутствует",true);  // выводим в лог ZP
}


// для запросов не возвращающих результата (INSERT/UPDATE/LOCK/UNLOCK/...) всегда используем метод query

// лочим таблицу accounts что бы только 1 поток работал с ней
// P.S вы должны лочить все таблицы и их алиасы с которыми собираетесь работать в рамках строго 1 потока ..
// в этом примере работа идет лишь с 1 таблицей, поэтому и лочится только она

db.query("LOCK TABLES accounts WRITE");

// получить набор строк/столбцов
// для получения набора данных всегда используем метод getAll .. второй параметр - разделитель столбцов в строках ... 
// его можно не указывать, по умолчанию он |

// берем 100 акков со status=0 (свободные), которые при этом дольше всех не брались ( ORDER BY check_time )

List<string> data = db.getAll("SELECT id, first_name, last_name FROM accounts WHERE status=0 ORDER BY check_time LIMIT 100","^");

List<string> ids = new List<string>(); // в этот список сохраним только id полученных данных
for(int i=0; i<data.Count; i++){
    var x = data[i].Split('^');
    project.SendInfoToLog("ID: "+x[0],true);
    project.SendInfoToLog("Имя: "+x[1],true);
    project.SendInfoToLog("Фамилия: "+x[2],true);
    project.SendInfoToLog("--------------",true);
    ids.Add(x[0]); // добавляем очередной id в список ids
}

// текущее юникс-время
int unixtime = (int)(DateTime.UtcNow - new DateTime(1970,1,1)).TotalSeconds;

// здесь, например, делаем чекинг id из списка ids на онлайн в ВК и наполняем список online теми id которые сейчас онлайн
List<string> online = new List<string>();

// меняем статус у тех кто online (только их мы берем в работу)
if ( online.Count > 0 )
db.query("UPDATE accounts SET status=1 WHERE id IN("+string.Join(",",online)+")");

// обновляем время проверки у всех взятых id
if ( ids.Count > 0 )
db.query("UPDATE accounts SET check_time="+unixtime.ToString()+" WHERE id IN("+string.Join(",",ids)+")");

// разлочиваем таблицу
db.query("UNLOCK TABLES");

// пример экранирования спецсимволов в строке (если не экранировать, то одинарная кавычка поломает наш запрос)
string first_name = db.escapeString("Д'артаньян");

// вставка новой записи
db.query("INSERT INTO accounts SET first_name='"+first_name+"'");

// получение Id только что вставленной записи
string acc_id = db.getOne("SELECT LAST_INSERT_ID()");
project.SendInfoToLog("ID вставленной записи: "+acc_id,true);

// завершаем сессию
db.close();

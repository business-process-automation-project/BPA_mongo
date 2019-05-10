using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace BPA_Version1
{
    class Program
    {
        public static MongoClient client = new MongoClient("mongodb://localhost:27017");

        static void Main(string[] args)
        {
            //Verbindung aufbauen 
            var database = client.GetDatabase("BPA");
            
            //Json einlesen 
            string text = System.IO.File.ReadAllText(@"C:\Users\D064979\Desktop\json\json.json");
            var document = BsonSerializer.Deserialize<BsonDocument>(text);
            var collection = database.GetCollection<BsonDocument>("Fragen");
            collection.InsertOne(document);

            //mehrere Strings einlesen Test
            string myjson = System.IO.File.ReadAllText(@"C:\Users\D064979\Desktop\json\json2.json");
            var doc = new BsonDocument {
                    { "values", BsonSerializer.Deserialize<BsonArray>(myjson) }
                };
            collection.InsertOne(doc); 


            //String einlesen mit Methode 
            string path = "C:\\Users\\D064979\\Desktop\\json\\json1.json";
            ReadJson("BPA", path, "Fragen"); 
            ShowDB();
            DeleteDB("BPA");
        }

        static void ShowDB() {
            var dbList = client.ListDatabases().ToList();
            Console.WriteLine("The list of databases are :");
            foreach (var item in dbList)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }

        static void DeleteDB(string name)
        {
            client.DropDatabase(name); 
        }

        static void ReadJson(string nameDb, string file, string collecName)
        {
            var database = client.GetDatabase(nameDb);
            string text = System.IO.File.ReadAllText(@file);
            var document = BsonSerializer.Deserialize<BsonDocument>(text);
            var collection = database.GetCollection<BsonDocument>(collecName);
            collection.InsertOne(document);
        }

      
    }
    
}

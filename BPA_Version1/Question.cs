using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace BPA_Version1
{
    class Question
    {
        [BsonId]
        public ObjectId Id
        {
            get;
            set;
        }

        [BsonElement("text")]
        public string text
        {
            get;
            set;
        }

        [BsonElement("ans_t")]
        public string ans_t
        {
            get;
            set;
        }

        [BsonElement("ans_f1")]
        public string ans_f1
        {
            get;
            set;
        }

        [BsonElement("ans_f2")]
        public string ans_f2
        {
            get;
            set;
        }
    }
}
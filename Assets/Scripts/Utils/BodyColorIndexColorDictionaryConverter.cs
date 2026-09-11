
using FYP.Character;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FYP.Utils
{
    public class SerializableColor
    {
        public float r, g, b, a;

        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    public class BodyColorIndexColorDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<BodyColorIndex, Color>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (Dictionary<BodyColorIndex, Color>)value;

            var serializableDictionary = new Dictionary<BodyColorIndex, SerializableColor>();

            foreach (var keyValuePair in dictionary)
            {
                serializableDictionary.Add(keyValuePair.Key, new SerializableColor(keyValuePair.Value));
            }

            serializer.Serialize(writer, serializableDictionary);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            var serializableDictionary = token.ToObject<Dictionary<BodyColorIndex, SerializableColor>>();

            var dictionary = new Dictionary<BodyColorIndex, Color>();

            foreach (var keyValuePair in serializableDictionary)
            {
                dictionary.Add(keyValuePair.Key, keyValuePair.Value.ToColor());
            }

            return dictionary;
        }
    }
}
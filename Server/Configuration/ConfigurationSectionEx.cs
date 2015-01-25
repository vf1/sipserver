using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;

namespace Server.Configuration
{
    abstract class ConfigurationSectionEx
        : ConfigurationSection
    {
        private Exception error;

        public readonly string SectionName;
        public readonly string FilePath;
        public readonly string DefaultXml;

        public ConfigurationSectionEx(string sectionName, string filePath)
        {
            this.SectionName = sectionName;
            this.FilePath = filePath;
            this.DefaultXml = new StreamReader(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    FindDefaultConfigurationResource()
                        )).ReadToEnd();
        }

        public void Load()
        {
            error = null;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

                if (File.Exists(FilePath) == false)
                {
                    File.WriteAllText(FilePath, DefaultXml);
                }
                else
                {
                    using (var reader = XmlReader.Create(
                        new StringReader(StriptXml(ReadXml()))))
                    {
                        DeserializeSection(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        public void Save()
        {
            string xml = SerializeSection(null, SectionName, ConfigurationSaveMode.Minimal);

            if (xml != null)
                File.WriteAllText(FilePath, "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + Environment.NewLine + xml);
        }

        public List<Exception> ListErrors()
        {
            var errors = new List<Exception>();

            ListErrors(errors);

            return errors;
        }

        public new void ListErrors(IList errors)
        {
            base.ListErrors(errors);

            if (error != null)
                errors.Add(error);
        }

        public string ReadXml()
        {
            return File.ReadAllText(FilePath);
        }

        public void WriteXml(string xml)
        {
            File.WriteAllText(FilePath, xml);
        }

        private static string StriptXml(string xml)
        {
            if (xml.StartsWith("<?xml"))
            {
                int index = xml.IndexOf('>');
                if (index > 0)
                    xml = xml.Substring(index + 1);
            }

            while (xml.StartsWith(Environment.NewLine))
                xml = xml.Substring(Environment.NewLine.Length);

            using (var reader = XmlReader.Create(new StringReader(xml)))
            {
                while (reader.Read() && reader.IsStartElement() == false) ;

                if (reader.EOF)
                    return xml;

                return reader.ReadOuterXml();
            }
        }

        public static IList<Exception> Validate<T>(string xml)
            where T : ConfigurationSectionEx, new()
        {
            var errors = new List<Exception>();

            try
            {
                Deserialize<T>(StriptXml(xml)).ListErrors(errors);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }

            return errors;
        }

        private static T Deserialize<T>(string xml)
            where T : ConfigurationSectionEx, new()
        {
            using (var reader = XmlReader.Create(new StringReader(xml)))
            {
                var section = new T();
                section.DeserializeSection(reader);

                return section;
            }
        }

        private string FindDefaultConfigurationResource()
        {
            foreach (var resource in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (resource.EndsWith(@"DefaultConfiguration.xml"))
                    return resource;
            }

            throw new Exception(@"DefaultConfiguration.xml not found");
        }
    }
}

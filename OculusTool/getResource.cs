using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace OculusTool
{  
    public class getResource
    {
        
        private static bool Get(String assemblyname, string fname)
        {
            if (File.Exists(fname))
                return true;
            else
            {
                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyname))
                {
                    if (input != null)
                    {

                        using (Stream output = File.Create(fname))
                        {
                            CopyStream(input, output);
                        }
                        if (System.IO.File.Exists(fname))
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
        }
        private static bool getcustom(String assemblyname, string fname)
        {
            if (File.Exists(fname))
                return true;
            else
            {
                using (Stream input = Assembly.GetEntryAssembly().GetManifestResourceStream(assemblyname))                    
                {                    
                    if (input != null)
                    {

                        using (Stream output = File.Create(fname))
                        {
                            CopyStream(input, output);
                        }
                        if (System.IO.File.Exists(fname))
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
        }
        private static void CopyStream(Stream input, Stream output)
        {            
            if (input != null)
            {           
            // Insert null checking here for production
            byte[] buffer = new byte[8192];            
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
            }
        }
                
        

        /// <summary>
        /// Retrieves a custom resource embedded in your project
        /// </summary>
        /// <param name="Namespace">The Program namespace the resource resides (Case Sensitive)</param>
        /// <param name="filename">the name of the file in your resources (Case sensitive)</param>
        /// <returns>Bool</returns>
        public static bool get(string Namespace , string filename)
        {
            return getcustom(Namespace + "." + filename, filename);
        }
    }
}

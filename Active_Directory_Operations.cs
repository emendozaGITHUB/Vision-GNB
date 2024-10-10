using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement; // Asegúrate de agregar la referencia al ensamblado de Active Directory
using System.Security.Principal;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;

namespace Active_Directory_Operations
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var usuarioAD = "Test-Enrique1Aranda";
            LogonHoursSetter.SetLogonHours("5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22", usuarioAD, "NonWorkingDays",false,true,true,true,true,true,false);
            LogonHoursSetter.SetLogonHours("1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,0", usuarioAD, "WorkingDays");
        }
    }
    public static class LogonHoursSetter
    {
     
        public static void SetLogonHours(
       string timeIn24Format, // Cambiado a string para aceptar entrada en formato "1,2,3,..."
       string identity,
       string nonSelectedDaysAre = "NonWorkingDays",
       bool sunday = false,
       bool monday = false,
       bool tuesday = false,
       bool wednesday = false,
       bool thursday = false,
       bool friday = false,
       bool saturday = false)
        {
            // Validar el rango de tiempo
            var times = timeIn24Format.Split(',').Select(t => int.Parse(t.Trim())).ToList();

            foreach (var time in times)
            {
                if (time < 0 || time > 23)
                {
                    throw new ArgumentOutOfRangeException(nameof(timeIn24Format), "El tiempo debe estar entre 0 y 23.");
                }
            }

            byte[] fullByte = new byte[21];
            Dictionary<int, string> fullDay = new Dictionary<int, string>();
            for (int i = 0; i <= 23; i++)
            {
                fullDay.Add(i, "0");
            }

            // Marcar las horas de inicio según el tiempo en formato 24
            foreach (var time in times)
            {
                fullDay[time] = "1";
            }

            string working = string.Join("", fullDay.Values);

            // Crear valores de días
            string sundayValue = "000000000000000000000000";
            string mondayValue = "000000000000000000000000";
            string tuesdayValue = "000000000000000000000000";
            string wednesdayValue = "000000000000000000000000";
            string thursdayValue = "000000000000000000000000";
            string fridayValue = "000000000000000000000000";
            string saturdayValue = "000000000000000000000000";

            if (nonSelectedDaysAre == "WorkingDays")
            {
                sundayValue = mondayValue = tuesdayValue = wednesdayValue = thursdayValue = fridayValue = saturdayValue = "111111111111111111111111";
            }

            // Ajustar según los días seleccionados
            if (sunday) sundayValue = working;
            if (monday) mondayValue = working;
            if (tuesday) tuesdayValue = working;
            if (wednesday) wednesdayValue = working;
            if (thursday) thursdayValue = working;
            if (friday) fridayValue = working;
            if (saturday) saturdayValue = working;

            // Construir la semana completa
            string allTheWeek = $"{sundayValue}{mondayValue}{tuesdayValue}{wednesdayValue}{thursdayValue}{fridayValue}{saturdayValue}";

            // Obtener la zona horaria
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

            string fixedTimeZoneOffset;

            if (offset.Hours < 0)
            {
                string timeZoneOffset = allTheWeek.Substring(0, 168 + offset.Hours);
                string timeZoneOffset1 = allTheWeek.Substring(168 + offset.Hours);
                fixedTimeZoneOffset = $"{timeZoneOffset1}{timeZoneOffset}";
            }
            else if (offset.Hours > 0)
            {
                string timeZoneOffset = allTheWeek.Substring(0, offset.Hours);
                string timeZoneOffset1 = allTheWeek.Substring(offset.Hours);
                fixedTimeZoneOffset = $"{timeZoneOffset1}{timeZoneOffset}";
            }
            else
            {
                fixedTimeZoneOffset = allTheWeek;
            }

            // Procesar el resultado binario
            string[] binaryResult = SeparateStringIntoChunks(fixedTimeZoneOffset,8).ToArray();//fixedTimeZoneOffset.Split(new[] { "00000000" }, StringSplitOptions.RemoveEmptyEntries);

            
        for (int i = 0; i < binaryResult.Length; i++)
        {
            char[] tempVar = binaryResult[i].ToCharArray();
            Array.Reverse(tempVar);
            string tempString = new string(tempVar);
            byte byteValue = Convert.ToByte(tempString, 2);
            fullByte[i] = byteValue;
        }


            // Actualizar el usuario en Active Directory

            UpdateUserAD(identity, fullByte);

            Console.WriteLine("All Done :)");
        }

        static List<string> SeparateStringIntoChunks(string input, int chunkSize)
        {
            List<string> chunks = new List<string>();

            for (int i = 0; i < input.Length; i += chunkSize)
            {
                // Extraer el fragmento de la cadena
                string chunk = input.Substring(i, Math.Min(chunkSize, input.Length - i));
                chunks.Add(chunk);
            }

            return chunks;
        }
        private static void UpdateUserAD(string userSamAccountName, byte[] logonHours)
        {

            // Dominio y credenciales (si son necesarios)
            string domain = "LDAP://DC=cycloudsec,DC=com";
            string username = "MIMAD";
            string password = "Pass@word1";



            // Conectar a Active Directory
            using (DirectoryEntry entry = new DirectoryEntry(domain,username,password))
            {
                using (DirectorySearcher searcher = new DirectorySearcher(entry))
                {
                    // Buscar el usuario por samAccountName
                    searcher.Filter = $"(&(objectClass=user)(samAccountName={userSamAccountName}))";

                    // Realizar la búsqueda
                    SearchResult result = searcher.FindOne();

                    if (result != null)
                    {
                        DirectoryEntry userEntry = result.GetDirectoryEntry();

                        // Actualizar el atributo logonHours con el nuevo valor
                        userEntry.Properties["logonHours"].Value = logonHours;

                        // Guardar los cambios
                        userEntry.CommitChanges();

                        Console.WriteLine($"Las horas de logon de {userSamAccountName} se han actualizado a Lunes a Viernes de 5:00 a 22:00.");
                    }
                    else
                    {
                        Console.WriteLine($"Usuario {userSamAccountName} no encontrado.");
                    }
                }
            }

        }
    }



}

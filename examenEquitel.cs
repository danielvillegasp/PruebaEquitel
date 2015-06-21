// Daniel Villegas
// 21 - 06 - 2015

using System;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Linq;

public class ContarReDesdeArchivo
{
    // Clase para contar ocurrencias de una expresión regular en un archivo.
    private string nombreArchivoLectura;
    private string nombreArchivoDestino;
    private string reCoincidencia;
    private string mensaje;
    private Semaphore pool;
    public ContarReDesdeArchivo(string aNombreArchivoLectura, string aNombreArchivoDestino, string aReCoincidencia, string aMensaje, ref Semaphore rPool)
    {
        nombreArchivoLectura = aNombreArchivoLectura;
        nombreArchivoDestino = aNombreArchivoDestino;
        reCoincidencia = aReCoincidencia;
        mensaje = aMensaje;
        pool = rPool;
    }
    
    // Cuenta el numero de ocurrencias de la expresión regular y escribe la respuesta en la linea de comandos y en un archivo de texto.
    public void contar()
    {
        if(File.Exists(nombreArchivoLectura))
        {
            var cnt = 0;
            // La lectura del archivo es Thread Safe.
            string texto = File.ReadAllText(nombreArchivoLectura);
            // Cuenta del numero de ocurrencias.
            cnt = Regex.Matches(texto, reCoincidencia).Count;
            // Inicia espera para que se desocupe el recurso compartido (Archivo de texto de destino).
            pool.WaitOne();
            if(!File.Exists(nombreArchivoDestino))
            {
            // Si el archivo de destino no existe, crea un archivo nuevo con el nombre especificado.
                using(StreamWriter file= File.CreateText(nombreArchivoDestino))
                {
                    file.WriteLine("{0}{1:D}", mensaje, cnt);
                }
            }
            else
            {
            // Si el archivo existe, agrega linea al final.
                using(StreamWriter file= File.AppendText(nombreArchivoDestino))
                {
                    file.WriteLine("{0}{1:D}", mensaje, cnt);
                }
            }
            // Escribe en la terminal el mensaje que se escribió en el archívo de texto de destíno.
            Console.WriteLine("{0}{1:D}", mensaje, cnt);
            // Informa que se dejó de usar el recurso compartido (Archivo de texto).
            pool.Release();
        }
        else
        {
            // Si el archivo de lectura no existe, muestra en la terminal el siguiente texto.
            Console.WriteLine("No se encuentra el archivo {0}.", nombreArchivoLectura);
        }
    }
};

public class Programa
{
    public static int Main(string[] args)
    {
        if(args.Contains("-h"))
        {
            // Argumento de ayuda para información de uso.
            var instrucciones = new StringBuilder();
            instrucciones.Append("\nSi no se usan argumentos se asume que el archivo de\n");
            instrucciones.Append("texto se llama ./texto.txt y el archivo de salida\n");
            instrucciones.Append("./resultado.txt, si se especifícan los nombres, el\n");
            instrucciones.Append("primero equivale al archvivo que se lee y el segundo al\n");
            instrucciones.Append("archivo de salida.\n");
            Console.Write(instrucciones);
            return 0;
        }
        // Inicialización nombre archivo de texto para lectura y nombre de archivo de destíno.
        var nombreArchivo=@"./texto.txt";
        var nombreArchivoDestino=@"./resultado.txt";
        if(args.Length == 1)
        {
            // Argumento de nombre de archivo de texto.
            nombreArchivo = args[0];
        }
        else if(args.Length==2)
        {
            // Argumento de nombre de archivo de salida.
            nombreArchivo = args[0];
            nombreArchivoDestino = args[1];
        }
        /* Inicialización expresiones regulares y mensajes en un arreglo bidimensional de strings.
            \b\w+n\b : busca un grupo de uno o mas caracteres de palabra (word characters) seguidos por una n y que se encuentren limitados por limites de palabras (\b).
            (([\-_\('’""]*\b\w+\b[\s\),:;\-_'’""]*){16,})\.[ ] : Busca un grupo de 16 o mas palabras donde la separación entre dos palabras puede estar formada por diferentes
            combinaciones de caracteres " ( ) , ; :, etc. que terminen por un punto seguído por un espacio ([ ]), se utiliza [ ] en vez de \s para no capturar el fin de linea.
            \.\r : Busca puntos seguidos por un "carriage return" (punto y aparte), esto equivale al numero de parrafos.
            [^nN \p{P}] : coincide con cualquier caracter excepto n N, espacio y signos de puntuacion.
        */
        string[,] reStr = {{@"\b\w+n\b", "El numero de palabras que terminan en N o n es: "},
                           {@"(([\-_\('’""]*\b\w+\b[ \),:;\-_'’""]*){16,})\.[ ]", "El numero de frases con mas de 15 palabras es: "},
                           {@"\.\r", "El numero parrafos es: "},
                           {@"[^nN\s\p{P}]", "El numero de caracteres alfanumericos diferentes a n y N es: "}};
        int nOps = reStr.GetLength(0);
        Thread[] threadArray = new Thread[nOps];
        // Se inicializa el semaforo desocupado para una petición concurrente.
        Semaphore sPool = new Semaphore(1, 1);
        for(var i=0; i<nOps; i++)
        {
            // Inicialización objeto para contar ocurrencias de expresiones regulares.
            ContarReDesdeArchivo oCnt = new ContarReDesdeArchivo(nombreArchivo, nombreArchivoDestino, reStr[i, 0], reStr[i, 1], ref sPool);
            // Creación del thread que correrá el metodo oCnt.contar.
            threadArray[i] = new Thread(new ThreadStart(oCnt.contar));
        }
        for(var i=0; i<nOps; i++)
        {
            // Inicio de los threads.
            threadArray[i].Start();
            while(!threadArray[i].IsAlive);
        }
        for(var i=0; i<nOps; i++)
        {
            // Finalización de los thread.
            threadArray[i].Join();
        }
        return 0;
    }     
}
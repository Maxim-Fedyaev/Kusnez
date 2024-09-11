using System;
using System.IO.Ports;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using Windows.UI.Input.Inking;
using kuznechik;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Windows.ApplicationModel.Activation;
using System.Collections.Generic;

public class PortChat
{
    const int Length_in_data = 256;

    static List<byte> list = new List<byte>();
    static byte[] Readmessage = new byte[256];
    static string Stringmessage;
    static int Length;
    static SerialPort _serialPort;
    const int PAD_MODE_1 = 1;
    const int PAD_MODE_2 = 2;
    const int PAD_MODE_3 = 3;
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
        Thread readThread = new Thread(Read);
        // Создание нового объекта SerialPort с установками по умолчанию.
        _serialPort = new SerialPort();

        // Позволяем пользователю установить подходящие свойства.
        _serialPort.PortName = SetPortName(_serialPort.PortName);
        _serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);
        _serialPort.Parity = SetPortParity(_serialPort.Parity);
        _serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
        _serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
        _serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);

        // Установка таймаутов чтения/записи (read/write timeouts)
        _serialPort.ReadTimeout = 500;
        _serialPort.WriteTimeout = 500;

        Console.WriteLine("Прием или передача?\n 0 - прием, 1 - передача\n");
        switch (Console.ReadLine())
        {
            case "0":
                _serialPort.Open();
                _serialPort.Write("1");
                readThread.Start();
                Console.WriteLine("Ожидаем данные\n");
                Length = Length_in_data;
                readThread.Join();
                _serialPort.Close();
                Console.WriteLine("Исходный текст: ");
                for (int i = 0; i < Length; i++)
                {
                    Console.Write("0x{0:x} ", Readmessage[i]);
                }
                Console.Write("\n");
                DecryptText(Readmessage);
                Console.ReadLine();
                return;

            case "1":
                Console.WriteLine("Что отправлять? (Нужно именно 256 символов)\n" +
                    "Например: 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345\n");
                Stringmessage = Console.ReadLine();
                byte[] Bytemessage = Encoding.UTF8.GetBytes(Stringmessage);
                for(int i = 0; i < Bytemessage.Length; i++)
                    list.Add(Bytemessage[i]);
                set_padding(get_size_pad(Bytemessage.Length, PAD_MODE_3), Bytemessage.Length, PAD_MODE_3);
                Console.WriteLine("Исходный текст: ");
                for (int i = 0; i < list.Count; i++)
                {
                    Console.Write("0x{0:x} ", list[i]);
                }
                Console.Write("\n");
                byte[] listArray = list.ToArray();
                Length = listArray.Length;
                EncryptText(listArray);
                Console.WriteLine("Отправляем зашифрованные данные\n");
                _serialPort.Open();
                _serialPort.Write(listArray, 0, listArray.Length);
                Console.WriteLine("Данные отправлены\n");
                readThread.Start();
                Console.WriteLine("Ожидаем расшифрованные данные от МК\n");
                readThread.Join();
                _serialPort.Close();
                Console.WriteLine("Принятые данные\n");
                for (int i = 0; i < listArray.Length; i++)
                {
                    Console.Write("0x{0:x} ", Readmessage[i]);
                }
                Console.Write("\n");
                Console.ReadLine();
                return;
        }

    }

    public static void EncryptText(byte[] listArray)
    {
        KuznyechikEncrypt Kuz = new KuznyechikEncrypt();                            //Создание экземпляра класса Кузнечик 
        byte[] password = { 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
                            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                            0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10,
                            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
        Kuz.SetKey(password);
        byte[] EncryptedText = Kuz.Encrypt(listArray);
        Console.WriteLine("Зашифрованный текст: ");
        for (int i = 0; i < EncryptedText.Length; i++)
        {
            Console.Write("0x{0:x} ", EncryptedText[i]);
        }
        Console.Write("\n");
    }
    public static void DecryptText(byte[] message)
    {
        KuznyechikEncrypt Kuz = new KuznyechikEncrypt();                            //Создание экземпляра класса Кузнечик 
        byte[] password = { 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff,
                            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                            0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10,
                            0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
        Kuz.SetKey(password);
        byte[] DecryptedFile = Kuz.Decrypt(message); //Получение массива байт расшифрованного файла

        Console.WriteLine("Расшифрованный текст: ");
        for (int i = 0; i < DecryptedFile.Length; i++)
        {
            Console.Write("0x{0:x} ", DecryptedFile[i]);
        }
        Console.Write("\n");
    }
 
    public static void Read()
    {   while (Readmessage[Length-1] == 0)
        {
            try
            {
                Thread.Sleep(1000);
                _serialPort.Read(Readmessage, 0, Length);
            }
            catch (TimeoutException) { }
        }
    }

    public static string SetPortName(string defaultPortName)
    {
        string portName;

        Console.WriteLine("Available Ports:");
        foreach (string s in SerialPort.GetPortNames())
        {
            Console.WriteLine(" {0}", s);
        }

        Console.Write("COM port({0}): ", "COM4");
        portName = Console.ReadLine();

        if (portName == "")
        {
            portName = "COM4";
        }
        return portName;
    }

    public static int SetPortBaudRate(int defaultPortBaudRate)
    {
        string baudRate;

        Console.Write("Baud Rate({0}): \n", defaultPortBaudRate);
        //baudRate = Console.ReadLine();

        //if (baudRate == "")
        //{
            baudRate = defaultPortBaudRate.ToString();
        //}

        return int.Parse(baudRate);
    }

    public static Parity SetPortParity(Parity defaultPortParity)
    {
        string parity;

        //Console.WriteLine("Available Parity options:");
        //foreach (string s in Enum.GetNames(typeof(Parity)))
        //{
        //    Console.WriteLine(" {0}", s);
        //}

        Console.Write("Parity({0}): \n", defaultPortParity.ToString());
       // parity = Console.ReadLine();

       // if (parity == "")
        //{
            parity = defaultPortParity.ToString();
        //}

        return (Parity)Enum.Parse(typeof(Parity), parity);
    }

    public static int SetPortDataBits(int defaultPortDataBits)
    {
        string dataBits;

        Console.Write("Data Bits({0}): \n", defaultPortDataBits);
        //dataBits = Console.ReadLine();

        //if (dataBits == "")
        //{
            dataBits = defaultPortDataBits.ToString();
        //}

        return int.Parse(dataBits);
    }

    public static StopBits SetPortStopBits(StopBits defaultPortStopBits)
    {
        string stopBits;

        //Console.WriteLine("Available Stop Bits options:");
        //foreach (string s in Enum.GetNames(typeof(StopBits)))
        //{
        //    Console.WriteLine(" {0}", s);
        //}

        Console.Write("Stop Bits({0}): \n", defaultPortStopBits.ToString());
        //stopBits = Console.ReadLine();

        //if (stopBits == "")
        //{
            stopBits = defaultPortStopBits.ToString();
        //}

        return (StopBits)Enum.Parse(typeof(StopBits), stopBits);
    }

    public static Handshake SetPortHandshake(Handshake defaultPortHandshake)
    {
        string handshake;

        //Console.WriteLine("Available Handshake options:");
        //foreach (string s in Enum.GetNames(typeof(Handshake)))
        //{
        //    Console.WriteLine(" {0}", s);
        //}

        Console.Write("Handshake({0}): \n", defaultPortHandshake.ToString());
        //handshake = Console.ReadLine();

        //if (handshake == "")
        //{
            handshake = defaultPortHandshake.ToString();
        //}

        return (Handshake)Enum.Parse(typeof(Handshake), handshake);
    }

    static byte get_size_pad(Int64 size, byte pad_mode)
    {
        if (pad_mode == PAD_MODE_1)
            // Если дополнение для процедуры 1 не нужно, возвращаем 0
            if ((16 - (size % 16)) == 16)
                return 0;

        if (pad_mode == PAD_MODE_3)
            // Если дополнение для процедуры 3 не нужно, возвращаем 0
            if ((16 - (size % 16)) == 16)
                return 0;
        // Возвращаем длину дополнения
        return ((byte)(16 - (size % 16)));
    }

    static void set_padding(byte pad_size, Int64 size, byte pad_mode)
    {
        if (pad_size > 0)
        {
            if (pad_mode == PAD_MODE_1) // Для процедуры 1
            {
                for (Int64 i = size; i < size + pad_size; i++)
                    // Пишем все нули
                    list.Add(0x00);
            }
            if (pad_mode == PAD_MODE_2) // Для процедуры 2
            {
                // Пишем единичку в первый бит
                list.Add(0x80);
                for (Int64 i = size + 1; i < size + pad_size; i++)
                    // Далее заполняем все остальное нулями
                    list.Add(0x00);
            }
            if (pad_mode == PAD_MODE_3) // Для процедуры 3
            {
                // Пишем единичку в первый бит
                list.Add(0x80);
                for (Int64 i = size + 1; i < size + pad_size; i++)
                    // Далее заполняем все остальное нулями
                    list.Add(0x00);
            }
        }
    }
}
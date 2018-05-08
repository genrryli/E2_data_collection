using UnityEngine;
using System.IO.Ports;//引入IO库
using System.Threading;
using System;
using UnityEngine.UI;


public class get_data : MonoBehaviour
{
    //定义串口相关参数
    public string portname = "COM8";
    public int portspeed = 115200;
    private SerialPort ArduinoPort;

    //线程相关参数
    private Thread tport;
    private bool on_thread = false;

    //获取数据
    private double angle_x, angle_y, angle_z;
    private string str = null;
    private byte[] list = new byte[66];//新建一个数组，装下读取到的数据，长度66（两个数据包的长度）
    private byte[] data = new byte[10];//数组用于存储角度的数据

    //UI
    [Header("UI")]
    public Text x;
    public Text y;
    public Text z;

    void Start()
    {      
        ArduinoPort = new SerialPort("\\\\.\\"+portname,115200);//初始化串口
        ArduinoPort.Open();//打开串口
        Debug.Log("serial open");

        on_thread = true;
        if (ArduinoPort.IsOpen && on_thread)//如果串口打开了，即打开线程
        {
            tport = new Thread(new ThreadStart(writedata));//定义线程
            tport.Start();//打开新线程
        }
        Debug.Log("thread started");
    }

    void FixedUpdate()//每0.02秒更新一次
    {
        if (Time.frameCount % 120 == 0) { System.GC.Collect(); }//清理缓存 
        if (on_thread)
        {           
            angle_x = ((short)(data[2] << 8 | data[1] & 0xff)) / 32768.0f * 180;//角度计算公式
            angle_y = ((short)(data[4] << 8 | data[3] & 0xff)) / 32768.0f * 180;
            angle_z = ((short)(data[6] << 8 | data[5] & 0xff)) / 32768.0f * 180;

            x.text = angle_x.ToString("F2");
            y.text = angle_y.ToString("F2");
            z.text = angle_z.ToString("F2");
        }
    }

    void writedata()//新的线程的函数、专门读取数据
    {
        try
        {
            while (on_thread)//当线程打开时执行循环
            {
                str = "";//还原字符串
                int count = ArduinoPort.Read(list, 0, list.Length);//读取串口数据
                if (count < 0){ return;}//当收不到数据时跳出程序
                for (int i = 0; i < list.Length && i<count; i++)//监测数组内的每个数据
                {
                    if (list[i].ToString("X2") == "53")//监测到六进制为53的数据，此数据为角度数据的头部数据（角速度为52，加速度为51）
                    {
                        Debug.Log("get");
                        for (int j = 0; j < 10; j++) { str += list[i + j].ToString("X2"); data[j] = list[i + j]; }//以头部数据为开端，截取所有角度数据
                        break;
                    }
                }
                //Debug.Log(str);
                ArduinoPort.DiscardOutBuffer();//清理缓存
                ArduinoPort.DiscardInBuffer();
            }
        }
        finally{ close_port(); }//异常时跳出
    }

   void OnApplicationQuit()
    {
        close_port();        
    }

    public void close_port()
    {
        on_thread = false;//跳出死循环
        if (tport.IsAlive) { tport.Abort(); }//关闭线程
        if (ArduinoPort.IsOpen) { ArduinoPort.Close();}
        Debug.Log("---thread killed---port closed---");
    }
}


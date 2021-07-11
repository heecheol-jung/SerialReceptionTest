# Serial Reception Test
- RS-232 serial port reception test
- Every 2ms a device sends sensor data.

![](https://github.com/heecheol-jung/SerialReceptionTest/blob/main/doc/ubuntu_reception_interval1.png)
</br>Fig1. Ubuntu reception interval1

![](https://github.com/heecheol-jung/SerialReceptionTest/blob/main/doc/win_reception_interval1.png)
</br>Fig2. Windows 10 reception interval1

1 .NET serial port reception speed test
- Serial port speed : 115200*10 bps
</br>

1.1 Device
- NUCLEO-H7A3ZI-Q
- MPU-6050 sensor data
- Sensor data will be sent to a PC every 2ms.
- Host will recognize COM port as a USB virtual COM port.
</br>

1.2 PC1
1.2.1 Device specifications
- Processor	: Intel(R) Core(TM) i7-6700HQ CPU @ 2.60GHz   2.59 GHz
- Installed : RAM	32.0 GB (31.9 GB usable)
- System type : 64-bit operating system, x64-based processor

1.2.2 Windows specifications
- Edition	: Windows 10 Pro
- Version	: 20H2
- OS build : 19042.1083
</br>

1.3 PC2
1.3.1 Device specifications
- LIVA ZE PLUS
- Processor : Intel(R) Celeron(R) CPU 3965U @ 2.20GHz
- Memory : 8 GB

1.3.2 Ubuntu specifications
- 18.04.4 LTS
</br>

2 Measurement
- A device will send event data to a PC every 2ms.</br>
  Parsing end time will be the received time on the PC.
</br>  

3 Serial port speed
- 115200x16 bps is OK on Windows 10.
- Serial port open failed for 115200x16 bps on Ubuntu.</br>
  115200x10 bps is OK on Ubuntu.
- 115200x10 bps fo Windows 10 and Ubuntu.
</br>

4 Projects
4.1 H7A3ZITxQ_MPU6050
- Firmware project fo send MPU6050 acceleration/gyro data.

4.2 SerialReceptionTest
4.2.1 SerialReceptionTest
- Sensor data reception application(Console application, Windows and Linux)
- Database : LiteDB

4.2.2 SerialReceptionDataViewer
- Sensor data reception inverval viewer(WPF applicaion, Windows only)
- Test reuslt data directory : SerialReceptionTest\SerialReceptionTest\data</br>
  linux_test_history.db : sensor data recptions history on Linux</br>
  win_test_history.db : sensor data recptions history on Windows 10

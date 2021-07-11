1. .NET serial port reception speed test
- Serial port speed : 115200*10 bps


1.1 Device
- NUCLEO-H7A3ZI-Q
- MPU-6050 sensor data
- Sensor data will be sent to a PC every 2ms.
- Host will recognize COM port as a USB virtual COM port.


1.2 PC1
1.2.1 Device specifications
- Processor	: Intel(R) Core(TM) i7-6700HQ CPU @ 2.60GHz   2.59 GHz
- Installed : RAM	32.0 GB (31.9 GB usable)
- System type : 64-bit operating system, x64-based processor

1.2.2 Windows specifications
- Edition	: Windows 10 Pro
- Version	: 20H2
- OS build : 19042.1083


1.3 PC2
1.3.1 Device specifications
- LIVA ZE PLUS
- Processor : Intel(R) Celeron(R) CPU 3965U @ 2.20GHz
- Memory : 8 GB

1.3.2 Ubuntu specifications
- 18.04.4 LTS


2. 측정
- Every 2ms a device will send event data to a PC.
  Parsing end time will be the received time on the PC.
  

3. Serial port speed
- 115200*16 bps is OK on Windows 10.
- Serial port open failed for 115200*16 bps on Ubuntu.
  115200*10 bps is OK on Ubuntu.
- 115200*10 bps fo Windows 10 and Ubuntu.


4. Projects
4.1 H7A3ZITxQ_MPU6050
- Firmware project fo send MPU6050 acceleration/gyro data.

4.2 SerialReceptionTest
4.2.1 SerialReceptionTest
- Sensor data reception application(Console application, Windows and Linux)
- Database : LiteDB

4.2.2 SerialReceptionDataViewer
- Sensor data reception inverval viewr(WPF applicaion, Windows only)
- Test reuslt data directory : SerialReceptionTest\SerialReceptionTest\data
  linux_test_history.db : sensor data recptions history on Linux
  win_test_history.db : sensor data recptions history on Windows 10
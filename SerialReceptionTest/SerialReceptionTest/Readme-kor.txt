1. .NET시리얼 포트 수신속도 측정 테스트
- 시리얼 포트 속도 : 115200*10 bps


1.1 Device
- NUCLEO-H7A3ZI-Q
- MPU-6050센서 데이터
- 2ms마다 센서 데이터를 PC로 전송
- 호스트에서는 USB가상 컴포트로 인식


1.2 PC1
1.2.1 Device specifications
- Device name :	ASUS-HCJUNG
- Processor	: Intel(R) Core(TM) i7-6700HQ CPU @ 2.60GHz   2.59 GHz
- Installed : RAM	32.0 GB (31.9 GB usable)
- Device ID	: E25B8E9D-502E-4EED-85D1-880591A7B282
- Product ID : 00331-20300-00000-AA198
- System type : 64-bit operating system, x64-based processor
- Pen and touch :	No pen or touch input is available for this display

1.2.2 Windows specifications
- Edition	: Windows 10 Pro
- Version	: 20H2
- Installed on : ‎3/‎16/‎2021
- OS build : 19042.1083
- Experience : Windows Feature Experience Pack 120.2212.3530.0


1.3 PC2
1.3.1 Device specifications
- LIVA ZE PLUS
- Processor : Intel(R) Celeron(R) CPU 3965U @ 2.20GHz
- Memory : 8 GB
- Disk : 1TB

1.3.2 Ubuntu specifications
- 18.04.4 LTS


2. 측정
- Device에서 PC로 2ms마다 event 데이터 송신
  PC에서 몇 ms마다 event파싱결과를 내는지 측정 



3. 시리얼속도
- Windows 10에서는 115200*16 bps로 해도 시리얼 포트를 열수 있다.
- 우분투에서 115200*16 bp로 설정하면 포트를 열기가 실패한다. 115200*10 bps로 하면 포트 열기가 가능하다.
- Windows 10과 우분투 모두 115200*10 bps로 테스트한다.
 

4. Projects
4.1 H7A3ZITxQ_MPU6050
- MPU6050 가속도/자이로 데이터 송신용 펌웨어.

4.2 SerialReceptionTest
4.2.1 SerialReceptionTest
- 센서데이터 수신 어플리케이션(콘솔 프로그램, Window와 리눅스용)
- Database : LiteDB

4.2.2 SerialReceptionDataViewer
- 센서데이터 수신 주기 뷰여 어플리케이션(WPF 프로그램, Window전용)
- 테트결과 데이터 디렉토리 : SerialReceptionTest\SerialReceptionTest\data
  linux_test_history.db : 리눅스에서 센서데이터 수신기록
  win_test_history.db : 윈도우에서 센서데이터 수신기록
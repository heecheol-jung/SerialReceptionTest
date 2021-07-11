mpu6050_read_usart_send_2ms_115200x16_1.kvdat
- USART 속도 : 115200*16=1843200
- fl_mpu6050_read_data ~ HAL_UART_Transmit : 약 646.3us
  2ms마다 반복
  (646us + 2ms = 약 3ms 전송주기)

mpu6050_read_usart_send_1ms_115200x16_1.kvdat
- USART 속도 : 115200*16=1843200
- fl_mpu6050_read_data ~ HAL_UART_Transmit : 약 646.3us
  1ms마다 반복
  (646us + 1ms = 약 2ms 전송주기)

mpu6050_read_usart_send_0ms_115200x16_1.kvdat
- USART 속도 : 115200*16=1843200
- fl_mpu6050_read_data ~ HAL_UART_Transmit : 약 646.3us
  0ms마다 반복
  (646us + 0ms = 약 648us 전송주기)
  루프에서 usart수신큐 검사등 작업에 약 1.5us시간이 추가됨

mpu6050_read_usart_send_2ms_115200x10.kvdat
- USART speed : 115200*10
- fl_mpu6050_read_data ~ HAL_UART_Transmit : about 731.5us
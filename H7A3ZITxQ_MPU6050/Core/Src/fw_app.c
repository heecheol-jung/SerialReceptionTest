#include <stdio.h>
#include <string.h>
#include "i2c.h"
#include "fw_app.h"

FL_DECLARE_DATA fw_app_t g_app;

#if FW_APP_PARSER_CALLBACK == 1
static void on_message_parsed(const void* parser_handle, void* context);
#endif

FL_DECLARE(void) fw_app_init(void)
{
  memset(&g_app, 0, sizeof(g_app));

  // Serial port for message communication.
  g_app.proto_mgr.uart_handle = &huart3;
  g_app.proto_mgr.parser_handle.on_parsed_callback = on_message_parsed;
  g_app.proto_mgr.parser_handle.context = (void*)&g_app;

  g_app.mpu6050.i2c = &hi2c1;
  g_app.mpu6050.timeout = 1000;
  g_app.mpu6050.ready = FL_FALSE;
}

FL_DECLARE(void) fw_app_hw_init(void)
{
  // TODO : Device id setting(DIP switch, flash storage, ...).
  g_app.device_id = 1;

  // GPIO output pin for debugging.
//  HAL_GPIO_WritePin(DBG_OUT1_GPIO_Port, DBG_OUT1_Pin, GPIO_PIN_RESET);
//  HAL_GPIO_WritePin(DBG_OUT2_GPIO_Port, DBG_OUT2_Pin, GPIO_PIN_RESET);

  fl_mpu6050_hw_init(&g_app.mpu6050);

  // Message receive in interrupt mode.
  FW_APP_UART_RCV_IT(g_app.proto_mgr.uart_handle, g_app.proto_mgr.rx_buf, 1);

  g_app.ready = FL_TRUE;
}

FL_DECLARE(void) fw_app_systick(void)
{
  if (g_app.ready == FL_TRUE)
  {
    g_app.tick++;
    g_app.mpu6050_tick++;

    if (g_app.mpu6050.ready == FL_TRUE)
    {
      if (g_app.mpu6050_tick >= FW_APP_MPU6050_SAMPLE_INTERVAL)
      {
        g_app.read_mpu6050 = FL_TRUE;
        g_app.mpu6050_tick = 0;
      }
    }

    // Do some work every 1 second.
    if (g_app.tick >= FW_APP_ONE_SEC_INTERVAL)
    {
      // LED1 toggle.
      HAL_GPIO_TogglePin(LD1_GPIO_Port, LD1_Pin);
      g_app.tick = 0;

      //->Instance->ISR
      //printf("USART_ISR_RXNE_RXFNE : %d\r\n", __HAL_UART_GET_FLAG(&huart3, USART_ISR_RXNE_RXFNE));
      //printf("USART_ISR_RXNE_RXFNE : 0x%08X\r\n", huart3.Instance->ISR);
      //printf("Q count : %d\r\n", fl_q_count(&g_app.proto_mgr.q));

//      if (g_app.mpu6050.ready == FL_TRUE)
//      {
//        g_app.read_mpu6050 = FL_TRUE;
//      }
    }
  }
}

#if FW_APP_PARSER_CALLBACK == 1
static void on_message_parsed(const void* parser_handle, void* context)
{
  fl_bin_msg_parser_t*    bin_parser = (fl_bin_msg_parser_t*)parser_handle;
  fw_app_proto_manager_t* proto_mgr = &((fw_app_t*)context)->proto_mgr;
  fl_bin_msg_header_t*    header = (fl_bin_msg_header_t*)&bin_parser->buf[1];
  fl_bin_msg_full_t*      tx_msg_full = (fl_bin_msg_full_t*)proto_mgr->out_buf;
  fl_bin_msg_full_t*      rx_msg_full = (fl_bin_msg_full_t*)bin_parser->buf;

  // Ignore the parsed message.
  if (header->device_id != ((fw_app_t*)context)->device_id)
  {
    return;
  }

  tx_msg_full->header.device_id = header->device_id;
  tx_msg_full->header.message_id = header->message_id;
  tx_msg_full->header.flag1.sequence_num = header->flag1.sequence_num;
  printf("Seq : %d\r\n", tx_msg_full->header.flag1.sequence_num);
  tx_msg_full->header.flag1.return_expected = FL_FALSE;
  tx_msg_full->header.flag2.error = FL_OK;

  switch (header->message_id)
  {
    case FL_MSG_ID_READ_HW_VERSION:
    {
      fl_hw_ver_t* hw_ver = (fl_hw_ver_t*)&(tx_msg_full->payload);
      sprintf(hw_ver->version, "%d.%d.%d", FW_APP_HW_MAJOR, FW_APP_HW_MINOR, FW_APP_HW_REVISION);
      proto_mgr->out_length = fl_bin_msg_build_response((uint8_t*)proto_mgr->out_buf, sizeof(proto_mgr->out_buf));
      break;
    }

    case FL_MSG_ID_READ_FW_VERSION:
    {
      fl_fw_ver_t* fw_ver = (fl_fw_ver_t*)&(tx_msg_full->payload);
      sprintf(fw_ver->version, "%d.%d.%d", FW_APP_FW_MAJOR, FW_APP_FW_MINOR, FW_APP_FW_REVISION);
      proto_mgr->out_length = fl_bin_msg_build_response((uint8_t*)proto_mgr->out_buf, sizeof(proto_mgr->out_buf));
      break;
    }

    case FL_MSG_ID_READ_ACCELGYRO:
    {
      if (g_app.mpu6050_data_count == 0)
      {
        fl_accel_gyro_read_t* mpu6050_read = (fl_accel_gyro_read_t*)&(rx_msg_full->payload);
        fl_accel_gyro_read_resp_t* mpu6050_read_resp = (fl_accel_gyro_read_resp_t*)&(tx_msg_full->payload);
        uint16_t count = fl_q_mpu6050_count(&g_app.mpu6050_q);

        //printf("Req cont : %d, Q count : %d\r\n", mpu6050_read->request_samples, count);
        mpu6050_read_resp->id = mpu6050_read->id;
        if (mpu6050_read->request_samples < count)
        {
          count = mpu6050_read->request_samples;
        }
        mpu6050_read_resp->actual_samples = count;
        g_app.mpu6050_data_count = count;

//        mpu6050_read_resp->actual_samples = 1;
//        g_app.mpu6050_data_count = 1;

        proto_mgr->out_length = fl_bin_msg_build_response((uint8_t*)proto_mgr->out_buf, sizeof(proto_mgr->out_buf));

        // TODO : Send MPU-6050 read response.
        // TODO : Send measured data.
      }
      break;
    }

    case FL_MSG_ID_START_ACCELGYRO:
    {
      fl_accel_gyro_start_t* gyro_start = (fl_accel_gyro_start_t*)&(rx_msg_full->payload);
      g_app.start_accelgyro = gyro_start->start_stop;

      if (g_app.start_accelgyro == FL_MSG_ACCELGYRO_START)
      {
        HAL_GPIO_WritePin(LD2_GPIO_Port, LD2_Pin, GPIO_PIN_SET);
      }
      else
      {
        HAL_GPIO_WritePin(LD2_GPIO_Port, LD2_Pin, GPIO_PIN_RESET);
      }

      //printf("Accel gyro start : %d\r\n", g_app.start_accelgyro);

      proto_mgr->out_length = fl_bin_msg_build_response((uint8_t*)proto_mgr->out_buf, sizeof(proto_mgr->out_buf));
      break;
    }
  }

  if (proto_mgr->out_length > 0)
  {
    HAL_UART_Transmit(proto_mgr->uart_handle, proto_mgr->out_buf, proto_mgr->out_length, FW_APP_PROTO_TX_TIMEOUT);
  }
  proto_mgr->out_length = 0;
}
#endif

FL_DECLARE(void) fw_app_send_accelgyro_data(void)
{
  if (g_app.mpu6050_data_count > 0)
  {
    uint32_t tick = HAL_GetTick();

//    printf("Data count : %d\r\n", g_app.mpu6050_data_count);

    while (g_app.mpu6050_data_count > 0)
    {
      fl_mpu6050_data_t data;
      fl_q_mpu6050_pop(&g_app.mpu6050_q, &data);

      memcpy(g_app.proto_mgr.out_buf, &data, sizeof(fl_mpu6050_data_t));
//      printf("Ax : %.3f, Ay : %.3f, Az : %.3f, Gx : %.3f, Gy : %.3f : Gz : %.3f\r\n",
//          ((fl_mpu6050_data_t*)g_app.proto_mgr.out_buf)->ax, ((fl_mpu6050_data_t*)g_app.proto_mgr.out_buf)->ay, ((fl_mpu6050_data_t*)g_app.proto_mgr.out_buf)->az,
//          ((fl_mpu6050_data_t*)g_app.proto_mgr.out_buf)->gx, ((fl_mpu6050_data_t*)g_app.proto_mgr.out_buf)->gy, ((fl_mpu6050_data_t*)g_app.proto_mgr.out_buf)->gz);

      g_app.proto_mgr.out_length = sizeof(fl_mpu6050_data_t);
//      printf("Length : %d\r\n", g_app.proto_mgr.out_length);

      HAL_UART_Transmit(g_app.proto_mgr.uart_handle, g_app.proto_mgr.out_buf, g_app.proto_mgr.out_length, FW_APP_PROTO_TX_TIMEOUT);
      g_app.proto_mgr.out_length = 0;

      g_app.mpu6050_data_count--;
      if ((HAL_GetTick() - tick) > 10)
      {
        break;
      }
      if (g_app.read_mpu6050 == FL_TRUE)
      {
        break;
      }
    }
  }
}

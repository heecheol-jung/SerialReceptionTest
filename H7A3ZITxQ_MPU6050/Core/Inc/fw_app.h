// fw_app.h
// Firmware application.

#ifndef FW_APP_H
#define FW_APP_H

#include "main.h"
#include "usart.h"
#include "gpio.h"

#include "fl_def.h"
#include "fl_queue.h"
#include "fl_message_def.h"
#include "fl_util.h"
#include "fl_mpu6050.h"
#include "fl_mpu6050_queue.h"

// Parser defines
#define FW_APP_BIN_PARSER           (1)

#define FW_APP_PARSER               FW_APP_BIN_PARSER

#define FW_APP_PARSER_CALLBACK      (1) // 0 : No parser callback, 1 : Parser callback

#include "fl_bin_message.h"
#include "fl_bin_message_parser.h"

#define FW_APP_HW_MAJOR             (0)
#define FW_APP_HW_MINOR             (0)
#define FW_APP_HW_REVISION          (1)

#define FW_APP_FW_MAJOR             (0)
#define FW_APP_FW_MINOR             (2)
#define FW_APP_FW_REVISION          (1)

#define FW_APP_UART_HANDLE                                UART_HandleTypeDef*
#define FW_APP_GPIO_HANDLE                                GPIO_TypeDef*
#define FW_APP_GPIO_TOGGLE(pin, port)                     HAL_GPIO_TogglePin(port, pin)
#define FW_APP_UART_RCV_IT(handle, buf, count)            HAL_UART_Receive_IT(handle, buf, count)
#define FW_APP_UART_TRANSMIT(handle, buf, count, timeout) HAL_GPIO_Transmit(handle, buf, count, timeout)

#define FW_APP_DEBUG_PACKET_LENGTH  (128)

#define FW_APP_ONE_SEC_INTERVAL     (999) // 1 second

#define FW_APP_PROTO_TX_TIMEOUT     (500)

#define FW_APP_MPU6050_SAMPLE_INTERVAL  (2)

FL_BEGIN_PACK1

typedef struct _fw_app_debug_manager
{
  FW_APP_UART_HANDLE    uart_handle;
  uint8_t               buf[FW_APP_DEBUG_PACKET_LENGTH];
  uint8_t               length;
} fw_app_debug_manager_t;

// Protocol manager
typedef struct _fw_app_proto_manager
{
  // UART handle.
  FW_APP_UART_HANDLE    uart_handle;

  // Buffer for received bytes.
  fl_queue_t            q;
  fl_bin_msg_parser_t   parser_handle;
  uint8_t               out_buf[FL_BIN_MSG_MAX_LENGTH];
  uint8_t               out_length;
  uint8_t               rx_buf[1];
} fw_app_proto_manager_t;


// Firmware application manager.
typedef struct _fw_app
{
  fl_bool_t               ready;
  uint32_t                device_id;
  // Current tick count.
  volatile uint32_t       tick;
  volatile uint32_t       mpu6050_tick;
  volatile uint8_t        read_mpu6050;

  uint16_t                mpu6050_data_count;
  uint8_t                 seq_num;
  volatile uint8_t        start_accelgyro;

  // Protocol manager.
  fw_app_proto_manager_t  proto_mgr;

  fl_mpu6050_t            mpu6050;
  fl_mpu6050_q_t          mpu6050_q;
} fw_app_t;

FL_END_PACK

FL_BEGIN_DECLS

FL_DECLARE_DATA extern fw_app_t g_app;

FL_DECLARE(void) fw_app_init(void);
FL_DECLARE(void) fw_app_hw_init(void);
FL_DECLARE(void) fw_app_systick(void);
FL_DECLARE(void) fw_app_send_accelgyro_data(void);
FL_END_DECLS

#endif

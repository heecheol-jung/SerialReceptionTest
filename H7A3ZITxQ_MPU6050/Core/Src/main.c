/* USER CODE BEGIN Header */
/**
  ******************************************************************************
  * @file           : main.c
  * @brief          : Main program body
  ******************************************************************************
  * @attention
  *
  * <h2><center>&copy; Copyright (c) 2021 STMicroelectronics.
  * All rights reserved.</center></h2>
  *
  * This software component is licensed by ST under BSD 3-Clause license,
  * the "License"; You may not use this file except in compliance with the
  * License. You may obtain a copy of the License at:
  *                        opensource.org/licenses/BSD-3-Clause
  *
  ******************************************************************************
  */
/* USER CODE END Header */
/* Includes ------------------------------------------------------------------*/
#include "main.h"
#include "i2c.h"
#include "usart.h"
#include "usb_otg.h"
#include "gpio.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */
#include <string.h>
#include "fw_app.h"
/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */

/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */
/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */

/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/

/* USER CODE BEGIN PV */
static int                _i, _temp; //, _len;
static uint8_t            _ch;
static fl_status_t        _ret;
/* USER CODE END PV */

/* Private function prototypes -----------------------------------------------*/
void SystemClock_Config(void);
/* USER CODE BEGIN PFP */
static void bin_message_processing(void);
/* USER CODE END PFP */

/* Private user code ---------------------------------------------------------*/
/* USER CODE BEGIN 0 */

/* USER CODE END 0 */

/**
  * @brief  The application entry point.
  * @retval int
  */
int main(void)
{
  /* USER CODE BEGIN 1 */
  fw_app_init();
  /* USER CODE END 1 */

  /* MCU Configuration--------------------------------------------------------*/

  /* Reset of all peripherals, Initializes the Flash interface and the Systick. */
  HAL_Init();

  /* USER CODE BEGIN Init */

  /* USER CODE END Init */

  /* Configure the system clock */
  SystemClock_Config();

  /* USER CODE BEGIN SysInit */

  /* USER CODE END SysInit */

  /* Initialize all configured peripherals */
  MX_GPIO_Init();
  MX_I2C1_Init();
  MX_USART3_UART_Init();
  MX_USB_OTG_HS_USB_Init();
  /* USER CODE BEGIN 2 */

  HAL_Delay(100);
  fw_app_hw_init();
  /* USER CODE END 2 */

  /* Infinite loop */
  /* USER CODE BEGIN WHILE */
  while (1)
  {
    bin_message_processing();

    if (g_app.start_accelgyro == FL_MSG_ACCELGYRO_START)
    {
      HAL_GPIO_WritePin(LD3_GPIO_Port, LD3_Pin, GPIO_PIN_SET);

      if (g_app.mpu6050.ready == FL_TRUE)
      {
        if (g_app.read_mpu6050 == FL_TRUE)
        {
          HAL_GPIO_WritePin(DBG_OUT1_GPIO_Port, DBG_OUT1_Pin, GPIO_PIN_SET);

          fl_mpu6050_read_data(&g_app.mpu6050, FL_MPU6050_I2C_ADDR);

          fl_bin_msg_full_t* _msg_full = (fl_bin_msg_full_t*)g_app.proto_mgr.out_buf;

          _msg_full->header.device_id = 1;
          _msg_full->header.message_id = FL_MSG_ID_ACCELGYRO_EVENT;
          _msg_full->header.flag1.sequence_num = g_app.seq_num++;
          _msg_full->header.flag1.return_expected = FL_FALSE;

          fl_accel_gyro_data_event_t* ag_data = (fl_accel_gyro_data_event_t*)&_msg_full->payload;
          ag_data->id = 1;
          ag_data->sample_count = 1;
          memcpy(ag_data->data, &g_app.mpu6050.raw_data, sizeof(fl_mpu6050_raw_data_t));

          g_app.proto_mgr.out_length = fl_bin_msg_build_event(g_app.proto_mgr.out_buf, sizeof(g_app.proto_mgr.out_buf));
          if (g_app.proto_mgr.out_length > 0)
          {
            HAL_UART_Transmit(g_app.proto_mgr.uart_handle, g_app.proto_mgr.out_buf, g_app.proto_mgr.out_length, FW_APP_PROTO_TX_TIMEOUT);
          }
          g_app.proto_mgr.out_length = 0;

          if (g_app.seq_num > FL_BIN_MSG_MAX_SEQUENCE)
          {
            g_app.seq_num = FL_BIN_MSG_MIN_SEQUENCE;
          }

          HAL_GPIO_WritePin(DBG_OUT1_GPIO_Port, DBG_OUT1_Pin, GPIO_PIN_RESET);
          g_app.read_mpu6050 = FL_FALSE;

  //        printf("%d,%d,%d,%d,%d,%d\r\n",
  //                    g_app.mpu6050.raw_data.ax, g_app.mpu6050.raw_data.ay, g_app.mpu6050.raw_data.az,
  //                    g_app.mpu6050.raw_data.gx, g_app.mpu6050.raw_data.gy, g_app.mpu6050.raw_data.gz);
  //        printf("%.2f,%.2f,%.2f,%.2f,%.2f,%.2f\r\n",
  //            g_app.mpu6050.data.ax, g_app.mpu6050.data.ay, g_app.mpu6050.data.az,
  //            g_app.mpu6050.data.gx, g_app.mpu6050.data.gy, g_app.mpu6050.data.gz);
        }
      }
      else
      {
        fl_mpu6050_hw_init(&g_app.mpu6050);
      }
      //g_app.start_accelgyro = FL_MSG_ACCELGYRO_STOP;
      //HAL_Delay(2);
    }
    else
    {
      HAL_GPIO_WritePin(LD3_GPIO_Port, LD3_Pin, GPIO_PIN_RESET);
    }

    /* USER CODE END WHILE */

    /* USER CODE BEGIN 3 */
  }
  /* USER CODE END 3 */
}

/**
  * @brief System Clock Configuration
  * @retval None
  */
void SystemClock_Config(void)
{
  RCC_OscInitTypeDef RCC_OscInitStruct = {0};
  RCC_ClkInitTypeDef RCC_ClkInitStruct = {0};

  /** Supply configuration update enable
  */
  HAL_PWREx_ConfigSupply(PWR_DIRECT_SMPS_SUPPLY);
  /** Configure the main internal regulator output voltage
  */
  __HAL_PWR_VOLTAGESCALING_CONFIG(PWR_REGULATOR_VOLTAGE_SCALE0);

  while(!__HAL_PWR_GET_FLAG(PWR_FLAG_VOSRDY)) {}
  /** Initializes the RCC Oscillators according to the specified parameters
  * in the RCC_OscInitTypeDef structure.
  */
  RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_HSI48|RCC_OSCILLATORTYPE_HSE;
  RCC_OscInitStruct.HSEState = RCC_HSE_BYPASS;
  RCC_OscInitStruct.HSI48State = RCC_HSI48_ON;
  RCC_OscInitStruct.PLL.PLLState = RCC_PLL_ON;
  RCC_OscInitStruct.PLL.PLLSource = RCC_PLLSOURCE_HSE;
  RCC_OscInitStruct.PLL.PLLM = 1;
  RCC_OscInitStruct.PLL.PLLN = 70;
  RCC_OscInitStruct.PLL.PLLP = 2;
  RCC_OscInitStruct.PLL.PLLQ = 4;
  RCC_OscInitStruct.PLL.PLLR = 2;
  RCC_OscInitStruct.PLL.PLLRGE = RCC_PLL1VCIRANGE_3;
  RCC_OscInitStruct.PLL.PLLVCOSEL = RCC_PLL1VCOWIDE;
  RCC_OscInitStruct.PLL.PLLFRACN = 0;
  if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK)
  {
    Error_Handler();
  }
  /** Initializes the CPU, AHB and APB buses clocks
  */
  RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK|RCC_CLOCKTYPE_SYSCLK
                              |RCC_CLOCKTYPE_PCLK1|RCC_CLOCKTYPE_PCLK2
                              |RCC_CLOCKTYPE_D3PCLK1|RCC_CLOCKTYPE_D1PCLK1;
  RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_PLLCLK;
  RCC_ClkInitStruct.SYSCLKDivider = RCC_SYSCLK_DIV1;
  RCC_ClkInitStruct.AHBCLKDivider = RCC_HCLK_DIV1;
  RCC_ClkInitStruct.APB3CLKDivider = RCC_APB3_DIV2;
  RCC_ClkInitStruct.APB1CLKDivider = RCC_APB1_DIV2;
  RCC_ClkInitStruct.APB2CLKDivider = RCC_APB2_DIV2;
  RCC_ClkInitStruct.APB4CLKDivider = RCC_APB4_DIV2;

  if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_7) != HAL_OK)
  {
    Error_Handler();
  }
}

/* USER CODE BEGIN 4 */
static void bin_message_processing(void)
{
  _temp = fl_q_count(&g_app.proto_mgr.q);

  if (_temp > 0)
  {
    //printf("Q count : %d\r\n", _temp);
    for (_i = 0; _i < _temp; _i++)
    {
      HAL_NVIC_DisableIRQ(USART3_IRQn);
      fl_q_pop(&g_app.proto_mgr.q, &_ch);
      HAL_NVIC_EnableIRQ(USART3_IRQn);
      _ret = fl_bin_msg_parser_parse(&g_app.proto_mgr.parser_handle, _ch, NULL);
      if (_ret != FL_BIN_MSG_PARSER_PARSING)
      {
        fl_bin_msg_parser_clear(&g_app.proto_mgr.parser_handle);
      }
    }
  }
}

void HAL_UART_RxCpltCallback(UART_HandleTypeDef *huart)
{
  if (huart == g_app.proto_mgr.uart_handle)
  {
    fl_q_push(&g_app.proto_mgr.q, g_app.proto_mgr.rx_buf[0]);
    FW_APP_UART_RCV_IT(g_app.proto_mgr.uart_handle, g_app.proto_mgr.rx_buf, 1);
  }
}

int _write(int file, char *ptr, int len)
{
 int DataIdx;

  for(DataIdx=0; DataIdx<len; DataIdx++)
  {
    ITM_SendChar(*ptr++);
  }
  return len;
}
/* USER CODE END 4 */

/**
  * @brief  This function is executed in case of error occurrence.
  * @retval None
  */
void Error_Handler(void)
{
  /* USER CODE BEGIN Error_Handler_Debug */
  /* User can add his own implementation to report the HAL error return state */
  __disable_irq();
  while (1)
  {
  }
  /* USER CODE END Error_Handler_Debug */
}

#ifdef  USE_FULL_ASSERT
/**
  * @brief  Reports the name of the source file and the source line number
  *         where the assert_param error has occurred.
  * @param  file: pointer to the source file name
  * @param  line: assert_param error line source number
  * @retval None
  */
void assert_failed(uint8_t *file, uint32_t line)
{
  /* USER CODE BEGIN 6 */
  /* User can add his own implementation to report the file name and line number,
     ex: printf("Wrong parameters value: file %s on line %d\r\n", file, line) */
  /* USER CODE END 6 */
}
#endif /* USE_FULL_ASSERT */

/************************ (C) COPYRIGHT STMicroelectronics *****END OF FILE****/

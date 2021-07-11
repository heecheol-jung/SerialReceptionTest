/*
 * fl_mpu6050.c
 *
 *  Created on: Jun 22, 2021
 *      Author: hcjung
 */
#include <stdio.h>
#include <string.h>
#include <math.h>
//#include "main.h"
#include "fl_mpu6050.h"

#define SENSORS_GAUSS_TO_MICROTESLA   (100) /**< Gauss to micro-Tesla multiplier */

FL_DECLARE(void) fl_mpu6050_init(fl_mpu6050_t *handle)
{
  memset(handle, 0, sizeof(fl_mpu6050_t));
}

FL_DECLARE(void) fl_mpu6050_hw_init(fl_mpu6050_t *handle)
{
  uint8_t data = 0;
  uint8_t i = 0;

  for (i = 0; i < 3; i++)
  {
    //HAL_I2C_Mem_Read (handle->i2c, FL_MPU6050_I2C_ADDR ,FL_MPU6050_REG_WHO_AM_I, 1, &data, 1, 1000);
    fl_mpu6050_read_byte(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_WHO_AM_I, &data);

    printf("MPU6060 : %d\r\n", data);
    if (data == 104)  // 0x68 will be returned by the sensor if everything goes well
    {
      printf("MPU6060 found.\r\n");
      // power management register 0X6B we should write all 0's to wake the sensor up
      data = 0;
      fl_mpu6050_write_byte(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_PWR_MGMT_1, data);

      // Set DATA RATE of 1KHz by writing SMPLRT_DIV register
      data = 0x07;
      fl_mpu6050_write_byte(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_SMPLRT_DIV, data);

      // Set accelerometer configuration in ACCEL_CONFIG Register
      // XA_ST=0,YA_ST=0,ZA_ST=0, FS_SEL=0 -> � 2g
      data = 0x00;
      fl_mpu6050_write_byte(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_ACCEL_CONFIG, data);

      // Set Gyroscopic configuration in GYRO_CONFIG Register
      // XG_ST=0,YG_ST=0,ZG_ST=0, FS_SEL=0 -> � 250 �/s
      data = 0x00;
      fl_mpu6050_write_byte(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_GYRO_CONFIG, data);

      handle->ready = FL_TRUE;
      break;
    }
    else
    {
      printf("MPU6060 NOT found.\r\n");
    }

    HAL_Delay(10);
  }
}

FL_DECLARE(fl_status_t) fl_mpu6050_read_byte(fl_mpu6050_t *handle, uint8_t i2c_addr, uint16_t reg_addr, uint8_t *data)
{
  fl_status_t ret = FL_OK;

  handle->buf[0] = reg_addr;

  if (HAL_I2C_Master_Transmit(handle->i2c, i2c_addr, handle->buf, 1, handle->timeout) == HAL_OK)
  {
    if (HAL_I2C_Master_Receive(handle->i2c, i2c_addr, handle->buf, 1, handle->timeout) == HAL_OK)
    {
      *data = handle->buf[0];
    }
    else
    {
      ret = FL_ERROR; // Register read error.
    }
  }
  else
  {
    ret = FL_ERROR; // Address write error.
  }

  return ret;
}

FL_DECLARE(fl_status_t) fl_mpu6050_read_multi_bytes(fl_mpu6050_t *handle, uint8_t i2c_addr, uint16_t reg_addr, uint8_t *data, uint16_t size)
{
  fl_status_t ret = FL_OK;

  handle->buf[0] = reg_addr;

  if (HAL_I2C_Master_Transmit(handle->i2c, i2c_addr, handle->buf, 1, handle->timeout) == HAL_OK)
  {
    if (HAL_I2C_Master_Receive(handle->i2c, i2c_addr, data, size, handle->timeout) != HAL_OK)
    {
      ret = FL_ERROR; // Register read error.
    }
  }
  else
  {
    ret = FL_ERROR; // Address write error.
  }

  return ret;
}

FL_DECLARE(fl_status_t) fl_mpu6050_write_byte(fl_mpu6050_t *handle, uint8_t i2c_addr, uint16_t reg_addr, uint8_t data)
{
  fl_status_t ret = FL_OK;

  handle->buf[0] = reg_addr;
  handle->buf[1] = data;

  if (HAL_I2C_Master_Transmit(handle->i2c, i2c_addr, handle->buf, 2, handle->timeout) != HAL_OK)
  {
    ret = FL_ERROR; // Write error.
  }

  return ret;
}

FL_DECLARE(fl_status_t) fl_mpu6050_read_raw_data(fl_mpu6050_t *handle, uint8_t i2c_addr)
{
  uint8_t data[6];

  fl_mpu6050_read_multi_bytes(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_ACCEL_XOUT_H, data, 6);

  handle->raw_data.ax = (int16_t)(data[1] | ((int16_t)data[0] << 8));
  handle->raw_data.ay = (int16_t)(data[3] | ((int16_t)data[2] << 8));
  handle->raw_data.az = (int16_t)(data[5] | ((int16_t)data[4] << 8));

  fl_mpu6050_read_multi_bytes(handle, FL_MPU6050_I2C_ADDR, FL_MPU6050_REG_GYRO_XOUT_H, data, 6);

  handle->raw_data.gx = (int16_t)(data[1] | ((int16_t)data[0] << 8));
  handle->raw_data.gy = (int16_t)(data[3] | ((int16_t)data[2] << 8));
  handle->raw_data.gz = (int16_t)(data[5] | ((int16_t)data[4] << 8));

  return FL_OK;
}

FL_DECLARE(fl_status_t) fl_mpu6050_read_data(fl_mpu6050_t *handle, uint8_t i2c_addr)
{
  fl_mpu6050_read_raw_data(handle, i2c_addr);

  handle->data.ax = (float)handle->raw_data.ax / FL_MPU6050_ACCEL_2G_SCALE;
  handle->data.ay = (float)handle->raw_data.ay / FL_MPU6050_ACCEL_2G_SCALE;
  handle->data.az = (float)handle->raw_data.az / FL_MPU6050_ACCEL_2G_SCALE;

  handle->data.gx = (float)handle->raw_data.gx / FL_MPU6050_GYRO_250_SCALE;
  handle->data.gy = (float)handle->raw_data.gy / FL_MPU6050_GYRO_250_SCALE;
  handle->data.gz = (float)handle->raw_data.gz / FL_MPU6050_GYRO_250_SCALE;

  return FL_OK;
}

/*
 * fl_mpu6050.h
 *
 *  Created on: Jun 22, 2021
 *      Author: hcjung
 */

#ifndef FL_MPU6050_H
#define FL_MPU6050_H

#include "main.h"
#include "fl_def.h"

// https://controllerstech.com/how-to-interface-mpu6050-gy-521-with-stm32/
#define FL_MPU6050_I2C_ADDR				      (0xD0)

#define FL_MPU6050_REG_SMPLRT_DIV       (0x19)
#define FL_MPU6050_REG_GYRO_CONFIG      (0x1B)
#define FL_MPU6050_REG_ACCEL_CONFIG     (0x1C)
#define FL_MPU6050_REG_ACCEL_XOUT_H     (0x3B)
#define FL_MPU6050_REG_TEMP_OUT_H       (0x41)
#define FL_MPU6050_REG_GYRO_XOUT_H      (0x43)
#define FL_MPU6050_REG_PWR_MGMT_1       (0x6B)
#define FL_MPU6050_REG_WHO_AM_I         (0x75)

#define FL_MPU6050_BUF_LEN    (8)

#define FL_MPU6050_ACCEL_2G_SCALE       (16384.0)
#define FL_MPU6050_ACCEL_4G_SCALE       (8192.0)
#define FL_MPU6050_ACCEL_8G_SCALE       (4096.0)
#define FL_MPU6050_ACCEL_16G_SCALE      (2048.0)

#define FL_MPU6050_GYRO_250_SCALE       (131.0)
#define FL_MPU6050_GYRO_500_SCALE       (65.5.0)
#define FL_MPU6050_GYRO_1000_SCALE      (32.8)
#define FL_MPU6050_GYRO_2000_SCALE      (16.4)

typedef struct _fl_mpu6050_raw_data
{
  int16_t ax;
  int16_t ay;
  int16_t az;
  int16_t gx;
  int16_t gy;
  int16_t gz;
} fl_mpu6050_raw_data_t;

typedef struct _fl_mpu6050_data
{
  float ax;
  float ay;
  float az;
  float gx;
  float gy;
  float gz;
} fl_mpu6050_data_t;

typedef struct _fl_mpu6050
{
  I2C_HandleTypeDef*    i2c;
  uint32_t              timeout;
  uint8_t               buf[FL_MPU6050_BUF_LEN];
  uint8_t               buf_len;
  uint8_t               ready;
  fl_mpu6050_raw_data_t raw_data;
  fl_mpu6050_data_t     data;
} fl_mpu6050_t;

FL_BEGIN_DECLS

FL_DECLARE(void) fl_mpu6050_init(fl_mpu6050_t *handle);
FL_DECLARE(void) fl_mpu6050_hw_init(fl_mpu6050_t *handle);
FL_DECLARE(fl_status_t) fl_mpu6050_read_byte(fl_mpu6050_t *handle, uint8_t i2c_addr, uint16_t reg_addr, uint8_t *data);
FL_DECLARE(fl_status_t) fl_mpu6050_read_multi_bytes(fl_mpu6050_t *handle, uint8_t i2c_addr, uint16_t reg_addr, uint8_t *data, uint16_t size);
FL_DECLARE(fl_status_t) fl_mpu6050_write_byte(fl_mpu6050_t *handle, uint8_t i2c_addr, uint16_t reg_addr, uint8_t data);
FL_DECLARE(fl_status_t) fl_mpu6050_read_raw_data(fl_mpu6050_t *handle, uint8_t i2c_addr);
FL_DECLARE(fl_status_t) fl_mpu6050_read_data(fl_mpu6050_t *handle, uint8_t i2c_addr);

FL_END_DECLS

#endif /* FL_I2C_H */

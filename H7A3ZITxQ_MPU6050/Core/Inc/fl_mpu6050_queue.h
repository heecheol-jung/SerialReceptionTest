/*
 * fl_mpu6050_queue.h
 *
 *  Created on: Jun 23, 2021
 *      Author: hcjung
 */

#ifndef FL_MPU6050_QUEUE_H
#define FL_MPU6050_QUEUE_H

#include "fl_def.h"
#include "fl_mpu6050.h"

#define FL_MPU6050_QUEUE_SIZE   (2000)

FL_BEGIN_PACK1

typedef struct _fl_mpu6050_q
{
  volatile fl_mpu6050_data_t  queue[FL_MPU6050_QUEUE_SIZE];

  volatile uint16_t            head;

  volatile uint16_t            tail;

  volatile uint16_t            count;
} fl_mpu6050_q_t;

FL_END_PACK

FL_BEGIN_DECLS

FL_DECLARE(void) fl_q_mpu6050_init(fl_mpu6050_q_t* q);
FL_DECLARE(uint16_t) fl_q_mpu6050_count(fl_mpu6050_q_t* q);
FL_DECLARE(fl_status_t) fl_q_mpu6050_push(fl_mpu6050_q_t* q, fl_mpu6050_data_t data);
FL_DECLARE(fl_status_t) fl_q_mpu6050_pop(fl_mpu6050_q_t* q, fl_mpu6050_data_t* data);

FL_END_DECLS

#endif /* FL_MPU6050_QUEUE_H */

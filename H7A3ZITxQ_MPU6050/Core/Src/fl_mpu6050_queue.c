/*
 * fl_mpu6050_queue.c
 *
 *  Created on: Jun 23, 2021
 *      Author: hcjung
 */

#include <string.h>
#include <stdlib.h>
#include "fl_mpu6050_queue.h"

FL_DECLARE(void) fl_q_mpu6050_init(fl_mpu6050_q_t* q)
{
  memset(q, 0, sizeof(fl_mpu6050_q_t));
}

FL_DECLARE(uint16_t) fl_q_mpu6050_count(fl_mpu6050_q_t* q)
{
  return q->count;
}

FL_DECLARE(fl_status_t) fl_q_mpu6050_push(fl_mpu6050_q_t* q, fl_mpu6050_data_t data)
{
  // Queue is full.
  if (q->count >= FL_MPU6050_QUEUE_SIZE)
  {
    return FL_ERROR;
  }

  if (q->head >= FL_MPU6050_QUEUE_SIZE)
  {
    q->head = 0;
  }

  q->queue[q->head++] = data;
  q->count++;

  return FL_OK;
}

FL_DECLARE(fl_status_t) fl_q_mpu6050_pop(fl_mpu6050_q_t* q, fl_mpu6050_data_t* data)
{
  // Queue is empty.
  if (q->count == 0)
  {
    return FL_ERROR;
  }

  //memcpy(data, &(q->queue[q->tail++]), sizeof(fl_mpu6050_data_t));
  *data = q->queue[q->tail++];
  q->count--;

  if (q->tail >= FL_MPU6050_QUEUE_SIZE)
  {
    q->tail = 0;
  }

  return FL_OK;
}

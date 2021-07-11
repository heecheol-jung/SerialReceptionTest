#include <string.h>
#include "fl_bin_message.h"
#include "fl_util.h"
#include "internal_util.h"

static void build_header_crc(uint8_t* packet_buf, uint16_t msg_size, uint8_t msg_type);
static fl_bool_t check_bin_msg_args(uint8_t* packet_buf, uint16_t packet_buf_len);

FL_DECLARE(uint8_t) fl_bin_msg_build_command(
  uint8_t* packet_buf,
  uint16_t packet_buf_len)
{
  uint16_t msg_size = sizeof(fl_bin_msg_header_t);

  if (check_bin_msg_args(packet_buf, packet_buf_len) != FL_TRUE)
  {
    return 0;
  }

  switch (((fl_bin_msg_full_t*)packet_buf)->header.message_id)
  {
    case FL_MSG_ID_READ_GPIO:
      msg_size += sizeof(fl_gpi_port_t);
      break;

    case FL_MSG_ID_WRITE_GPIO:
      msg_size += sizeof(fl_gpo_port_value_t);
      break;

    case FL_MSG_ID_READ_TEMPERATURE:
    case FL_MSG_ID_READ_HUMIDITY:
    case FL_MSG_ID_READ_TEMP_AND_HUM:
      msg_size += sizeof(fl_sensor_t);
      break;

    case FL_MSG_ID_BOOT_MODE:
      msg_size += sizeof(fl_boot_mode_t);
      break;

    case FL_MSG_ID_READ_WRITE_I2C:
    {
      fl_i2c_read_write_t* i2c_op = (fl_i2c_read_write_t*)&(((fl_bin_msg_full_t*)packet_buf)->payload);
      msg_size += (sizeof(fl_i2c_read_write_t) - FL_MSG_I2C_MAX_PAYLOAD_LEN);
      msg_size += i2c_op->buf_len;
      break;
    }

    case FL_MSG_ID_READ_ACCELGYRO:
      msg_size += sizeof(fl_accel_gyro_read_t);
      break;

    case FL_MSG_ID_START_ACCELGYRO:
      msg_size += sizeof(fl_accel_gyro_start_t);
      break;
  }

  build_header_crc(packet_buf, msg_size, FL_MSG_TYPE_COMMAND);

  // Packet length = stx(1 byte) + message size + crc size(2 byte) + etx size(1 byte).
  return (msg_size + 4);
}

FL_DECLARE(uint8_t) fl_bin_msg_build_response(
  uint8_t* packet_buf,
  uint16_t packet_buf_len)
{
  uint8_t msg_size = sizeof(fl_bin_msg_header_t);

  if (check_bin_msg_args(packet_buf, packet_buf_len) != FL_TRUE)
  {
    return 0;
  }

  if (((fl_bin_msg_full_t*)packet_buf)->header.flag2.error == FL_OK)
  {
    switch (((fl_bin_msg_full_t*)packet_buf)->header.message_id)
    {
      case FL_MSG_ID_READ_HW_VERSION:
      {
        fl_hw_ver_t* hw_ver = (fl_hw_ver_t*)&(((fl_bin_msg_full_t*)packet_buf)->payload);
        uint32_t len = strlen(hw_ver->version);
        if (len > sizeof(fl_hw_ver_t))
        {
          return 0;
        }
        msg_size += (uint8_t)len;
        break;
      }

      case FL_MSG_ID_READ_FW_VERSION:
      {
        fl_fw_ver_t* fw_ver = (fl_fw_ver_t*)&(((fl_bin_msg_full_t*)packet_buf)->payload);
        uint32_t len = strlen(fw_ver->version);
        if (len > sizeof(fl_fw_ver_t))
        {
          return 0;
        }
        msg_size += (uint8_t)len;
        break;
      }

      case FL_MSG_ID_READ_GPIO:
      {
        msg_size += sizeof(fl_gpi_port_value_t);
        break;
      }

      case FL_MSG_ID_READ_TEMPERATURE:
      {
        msg_size += sizeof(fl_temp_sensor_read_t);
        break;
      }

      case FL_MSG_ID_READ_HUMIDITY:
      {
        msg_size += sizeof(fl_hum_sensor_read_t);
        break;
      }

      case FL_MSG_ID_READ_TEMP_AND_HUM:
      {
        msg_size += sizeof(fl_temp_hum_sensor_read_t);
        break;
      }

      case FL_MSG_ID_READ_WRITE_I2C:
      {
        if (((fl_bin_msg_full_t*)packet_buf)->header.flag1.reserved == FL_FALSE)
        {
          fl_i2c_read_resp_t* i2c_op = (fl_i2c_read_resp_t*)&(((fl_bin_msg_full_t*)packet_buf)->payload);
          msg_size += (sizeof(fl_i2c_read_resp_t) - FL_MSG_I2C_MAX_PAYLOAD_LEN);
          msg_size += i2c_op->buf_len;
        }
        break;
      }

      case FL_MSG_ID_READ_ACCELGYRO:
      {
        msg_size += sizeof(fl_accel_gyro_read_resp_t);
        break;
      }
    }
  }

  build_header_crc(packet_buf, msg_size, FL_MSG_TYPE_RESPONSE);

  // Packet length = stx(1 byte) + message size + crc size(2 byte) + etx size(1 byte).
  return (msg_size + 4);
}

FL_DECLARE(uint8_t) fl_bin_msg_build_event(uint8_t* packet_buf, uint16_t packet_buf_len)
{
  uint8_t msg_size = sizeof(fl_bin_msg_header_t);

  if (check_bin_msg_args(packet_buf, packet_buf_len) != FL_TRUE)
  {
    return 0;
  }

  if (((fl_bin_msg_full_t*)packet_buf)->header.message_id == FL_MSG_ID_BUTTON_EVENT)
  {
    msg_size += sizeof(fl_btn_status_t);
  }
  else if (((fl_bin_msg_full_t*)packet_buf)->header.message_id == FL_MSG_ID_ACCELGYRO_EVENT)
  {
    fl_bin_msg_full_t* msg_full = (fl_bin_msg_full_t*)packet_buf;
    msg_size += (2 + msg_full->payload.sample_count * FL_MSG_ACCELGYRO_DATA_SIZE);
  }

  build_header_crc(packet_buf, msg_size, FL_MSG_TYPE_EVENT);

  // Packet length = stx(1 byte) + message size + crc size(2 byte) + etx size(1 byte).
  return (msg_size + 4);
}

static void build_header_crc(uint8_t* packet_buf, uint16_t msg_size, uint8_t msg_type)
{
  fl_bin_msg_full_t* msg_full = (fl_bin_msg_full_t*)packet_buf;

  msg_full->stx = FL_BIN_MSG_STX;

  // length field value = Message size - device ID field(4) - Length field(2) + CRC(2) + ETX(1)
  msg_full->header.length = msg_size - (sizeof(uint32_t) + sizeof(uint16_t)) + 3;

  msg_full->header.flag1.message_type = msg_type;

  *((uint16_t*)(&packet_buf[1] + msg_size)) = fl_crc_16(&packet_buf[1], msg_size);
  (&packet_buf[1] + msg_size)[2] = FL_BIN_MSG_ETX;
}

static fl_bool_t check_bin_msg_args(uint8_t* packet_buf, uint16_t packet_buf_len)
{
  if (packet_buf == NULL)
  {
    return FL_FALSE;
  }
  else if ((packet_buf_len == 0) ||
           (packet_buf_len < (sizeof(fl_bin_msg_full_t) - sizeof(fl_accel_gyro_data_event_t)))) // The shortest message : header only message.
  {
    return FL_FALSE;
  }

  return FL_TRUE;
}

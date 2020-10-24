#include <M5StickC.h>
#include <BluetoothSerial.h>

#define DOT_SIZE 2
#define FONT_SIZE 1
#define FONT_H (8 * DOT_SIZE)
#define SCREEN_W 160
#define INTERVAL 20
#define BAT_REPAINT_FRAMES (1000 / INTERVAL)
#define BAT_PER_MAX_V 4.1f
#define BAT_PER_MIN_V 3.3f
#define BAT_PER_INCLINATION (100.0f / (BAT_PER_MAX_V - BAT_PER_MIN_V))
#define BAT_PER_INTERCEPT   (-1 * BAT_PER_MIN_V * BAT_PER_INCLINATION)

BluetoothSerial btserial;
uint64_t chipid;
char chipname[256];

const int motor_pin = 32;
int freq = 10000;
int ledChannel = 0;
int resolution = 10;

int level = 0;
int frame = 0;

void repaint_status(int16_t row, uint16_t bg, const char *format, ...) {
  char buf[64];
  va_list va;
  va_start(va, format);
  vsprintf(buf, format, va);
  va_end(va);
  M5.Lcd.fillRect(0, FONT_H*row, SCREEN_W, FONT_H, bg);
  M5.Lcd.setCursor(0, FONT_H*row, FONT_SIZE);
  M5.Lcd.print(buf);
}

void setup() {
  chipid = ESP.getEfuseMac();
  sprintf(chipname, "M5Haptics_%04X", (uint16_t)(chipid >> 32));
  M5.begin();
  M5.Lcd.setRotation(3);
  M5.Lcd.setTextSize(DOT_SIZE);
  M5.Lcd.setTextColor(WHITE);
  M5.Lcd.fillScreen(BLACK);
  M5.Lcd.setCursor(0, 0, FONT_SIZE);
  M5.Lcd.printf("Name: %s\n", chipname);

  ledcSetup(ledChannel, freq, resolution);
  ledcAttachPin(motor_pin, ledChannel);

  repaint_status(4, BLUE, "Starting");
  btserial.begin(chipname);
  repaint_status(4, BLUE, "Waiting");
}

double get_bat_per() {
  float bat_v = M5.Axp.GetVbatData() * 1.1 / 1000;
  float bat_per = BAT_PER_INCLINATION * bat_v + BAT_PER_INTERCEPT;
  if (bat_per < 0.0f) bat_per = 0.0f;
  if (100.0f < bat_per) bat_per = 100.0f;
  return bat_per;
}

void repaint_bat_per() {
  double bat_per = get_bat_per();
  repaint_status(3, RED, "Bat: %3.0f%%", bat_per);
  //SerialBT.printf("%d\n", (int)(bat_per));
}

void loop() {
  if (btserial.available()) {
    String receiveData = btserial.readStringUntil('\n');
    sscanf(receiveData.c_str(), "%d\n", &level);
    ledcWrite(ledChannel, level); // 0 - 1023
    repaint_status(4, BLUE, "Level: %3d", level);
  }
  if (frame % BAT_REPAINT_FRAMES == 0) repaint_bat_per();
  frame++;
  delay(INTERVAL);
}

/*
    ClockTest.ino  -  Don Cross  -  2021-07-30
*/

const int PULSE_INPUT_PIN  =  2;    // GPS pulse input

static unsigned long _PulseCount;

void OnInputPulse()
{
    ++_PulseCount;
}

unsigned long PulseCount()
{
    noInterrupts();
    unsigned long count = _PulseCount;
    interrupts();
    return count;
}

void setup()
{
    pinMode(PULSE_INPUT_PIN, INPUT);
    Serial.begin(115200);
    attachInterrupt(digitalPinToInterrupt(PULSE_INPUT_PIN), OnInputPulse, FALLING);
}

void loop()
{
    unsigned long before = PulseCount();
    delay(1000);
    unsigned long after = PulseCount();
    Serial.print(after - before);
    Serial.println();
}


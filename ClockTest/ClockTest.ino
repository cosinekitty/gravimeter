/*
    ClockTest.ino  -  Don Cross  -  2021-07-30
*/

typedef signed char     int8;
typedef unsigned char   uint8;
typedef int             int16;
typedef unsigned int    uint16;
typedef long            int32;
typedef unsigned long   uint32;

//------------------------------------------------------------------------------------

const int GPS_INPUT_PIN  =  2;

//------------------------------------------------------------------------------------

class LinePrinter   // transmits lines of text and checksums over serial port
{
private:
    uint16 sum1;
    uint16 sum2;

    void Reset()
    {
        sum1 = 0xd3;
        sum2 = 0x95;
    }

    void Update(uint8 c)
    {
        // http://en.wikipedia.org/wiki/Fletcher%27s_checksum
        sum1 = (sum1 + (uint16)c) % 255;
        sum2 = (sum2 + sum1) % 255;
    }

    void SendHex(uint8 digit) const
    {
        if ((digit >= 0) && (digit <= 9))
            Serial.print((char)(digit + '0'));
        else if ((digit >= 0xa) && (digit <= 0xf))
            Serial.print((char)(digit - 0xa + 'a'));
        else
            Serial.print('!');      // this should never happen!
    }

    void SendChecksum() const
    {
        // Encode and transmit the checksum as hex digits.
        Serial.print('#');
        SendHex((uint8)((sum2 >> 4) & 0xf));
        SendHex((uint8)(sum2 & 0xf));
        SendHex((uint8)((sum1 >> 4) & 0xf));
        SendHex((uint8)(sum1 & 0xf));
    }

public:
    LinePrinter()
    {
        Reset();
    }

    void Print(char c)
    {
        Update((uint8)c);
        Serial.print(c);
    }

    void Print(const char *s)
    {
        while (*s != '\0')
        {
            Print(*s);
            ++s;
        }
    }

    void PrintLong(uint32 value)
    {
        uint32 power = 1000000000;
        while (power != 0)
        {
            char digit = ((char) ('0' + (value / power)%10));
            Print(digit);
            power /= 10;
        }
    }

    void EndLine()
    {
        SendChecksum();
        Serial.println();
        Reset();
    }
};

//------------------------------------------------------------------------------------

struct GpsData
{
    uint32  count;
    uint32  min_interval_us;
    uint32  max_interval_us;
};

const uint32 GPS_SETTLE_COUNT = 4;

class GpsClock
{
private:
    bool    is_initializing;
    uint32  count;
    uint32  prev_pulse_us;
    uint32  min_interval_us;
    uint32  max_interval_us;

public:
    void Reset()
    {
        is_initializing = true;
        count = 0;
        prev_pulse_us = 0;
        min_interval_us = 0;
        max_interval_us = 0;
    }

    bool IsReady() const
    {
        noInterrupts();
        bool ready = !is_initializing;
        interrupts();
        return ready;
    }

    void OnPulse()
    {
        uint32 pulse_us = micros();
        ++count;

        if (is_initializing && count < GPS_SETTLE_COUNT)
        {
            // Do nothing. We wait several pulses before considering the system settled.
        }
        else
        {
            // Use the lower-precision Arduino clock as a sanity check
            // for the GPS clock. We want to make sure we aren't missing pulses,
            // so track the minimum and maximum interval in microseconds between
            // each pulse. If they deviate too much, later I can add code to
            // signal a failure state (turn on red LED or something like that).
            uint32 interval = pulse_us - prev_pulse_us;

            if (is_initializing && count == GPS_SETTLE_COUNT)
            {
                // This is the very first interval, so initialize the min and max values.
                min_interval_us = max_interval_us = interval;
                is_initializing = false;
            }
            else
            {
                // On every subsequent interval, update min and max.
                if (interval < min_interval_us)
                    min_interval_us = interval;

                if (interval > max_interval_us)
                    max_interval_us = interval;
            }
        }

        prev_pulse_us = pulse_us;
    }

    GpsData Snapshot()
    {
        GpsData snap;

        noInterrupts();
        snap.count = count;
        snap.min_interval_us = min_interval_us;
        snap.max_interval_us = max_interval_us;
        interrupts();

        return snap;
    }
};

GpsClock TheGpsClock;

void OnGpsPulse()
{
    TheGpsClock.OnPulse();
}

//------------------------------------------------------------------------------------

void Report()
{
    LinePrinter lp;
    GpsData snap = TheGpsClock.Snapshot();
    lp.PrintLong(snap.count);
    lp.Print(' ');
    lp.PrintLong(snap.min_interval_us);
    lp.Print(' ');
    lp.PrintLong(snap.max_interval_us);
    lp.EndLine();
}

void Zero()
{
    noInterrupts();
    TheGpsClock.Reset();
    interrupts();
    while (!TheGpsClock.IsReady())
        delay(10);
    Report();
}

//------------------------------------------------------------------------------------

void setup()
{
    Serial.begin(115200);
    TheGpsClock.Reset();
    pinMode(GPS_INPUT_PIN, INPUT);
    attachInterrupt(digitalPinToInterrupt(GPS_INPUT_PIN), OnGpsPulse, FALLING);

    // Wait for the GPS clock signal to settle down and interval stats to be valid.
    while (!TheGpsClock.IsReady())
        delay(50);
}

void loop()
{
    if (Serial.available())
    {
        char c = Serial.read();
        switch (c)
        {
        case 'r':   Report();   break;
        case 'z':   Zero();     break;
        }
    }
}

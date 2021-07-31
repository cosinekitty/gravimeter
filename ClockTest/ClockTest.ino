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

    void PrintInteger(int16 value)
    {
        if (value == 0)
        {
            Print('0');
        }
        else
        {
            if (value < 0)
            {
                Print('-');
                value = -value;
            }

            bool foundNonZero = false;
            int16 power = 10000;
            while (power != 0)
            {
                char digit = ((char) ('0' + (value / power)%10));
                if (foundNonZero || (digit != '0'))
                {
                    foundNonZero = true;
                    Print(digit);
                }
                power /= 10;
            }
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


const int GPS_INPUT_PIN  =  2;    // GPS pulse input

static uint32 _GpsPulseCount;

void OnGpsPulse()
{
    ++_GpsPulseCount;
}

uint32 GpsPulseCount()
{
    noInterrupts();
    uint32 count = _GpsPulseCount;
    interrupts();
    return count;
}

//------------------------------------------------------------------------------------

void setup()
{
    pinMode(GPS_INPUT_PIN, INPUT);
    Serial.begin(115200);
    attachInterrupt(digitalPinToInterrupt(GPS_INPUT_PIN), OnGpsPulse, FALLING);
}

void loop()
{
    if (Serial.available())
    {
        char c = Serial.read();
        switch (c)
        {
        case 'r':
            Report();
            break;
        }
    }
}

void Report()
{
    LinePrinter lp;
    uint32 count = GpsPulseCount();
    lp.PrintLong(count);
    lp.EndLine();
}

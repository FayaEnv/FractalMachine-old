#include <iostream>
#include <map>
#include <string>
#include <cstdarg>

using namespace std;

// https://chryswoods.com/beginning_c++/lists.html

class DynVar {     
    public:             
        map<string, DynVar> Properties;

        // https://en.cppreference.com/w/cpp/utility/variadic
        // https://stackoverflow.com/questions/25392935/wrap-a-function-pointer-in-c-with-variadic-template
        // https://stackoverflow.com/questions/26575303/create-function-call-dynamically-in-c
        // https://softwareengineering.stackexchange.com/questions/360083/how-to-design-a-c-program-to-allow-for-runtime-import-of-functions
        // https://www.bfilipek.com/2018/06/any.html
        void Call(const char* fmt...) {
            va_list args;
            va_start(args, fmt);

            while (*fmt != '\0') {
                if (*fmt == 'd') {
                    int i = va_arg(args, int);
                    cout << i << '\n';
                }
                else if (*fmt == 'c') {
                    // note automatic conversion to integral type
                    int c = va_arg(args, int);
                    cout << static_cast<char>(c) << '\n';
                }
                else if (*fmt == 'f') {
                    double d = va_arg(args, double);
                    cout << d << '\n';
                }
                ++fmt;
            }

            va_end(args);
        }


        string Value() {

        }
};
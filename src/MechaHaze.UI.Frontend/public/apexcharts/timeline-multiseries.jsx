import React from 'react';
import ReactApexChart from 'react-apexcharts';

import moment from 'moment';

let tickToDate = (ticks) => {
    return (ticks - 621355968000000000) / 10000;
};

export class Timeline extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            options: {
                plotOptions: {
                    bar: {
                        horizontal: true
                    }
                },
                dataLabels: {
                    enabled: true,
                    formatter: function (val) {
                        const a = moment(val[0]);
                        const b = moment(val[1]);
                        const diff = b.diff(a, 'milliseconds', true);
                        return diff + (diff > 1 ? ' ms' : ' ms')
                    }
                },
                xaxis: {
                    type: 'datetime'
                },
                legend: {
                    position: 'top'
                }
            },
            series: [
                {
                    name: 'Temp 1',
                    data: [
                        {
                            x: 'Design',
                            y: [
                                tickToDate(637122821057885758),
                                    tickToDate(637122822098610580)
                            ]
                        },
                        {
                            x: 'Code',
                            y: [
                                tickToDate(637122823098610580),
                                    tickToDate(637122824098610580)
                            ]
                        },
                        {
                            x: 'Test',
                            y: [
                                tickToDate(637122825098610580),
                                    tickToDate(637122826098610580)
                            ]
                        }
                    ]
                },
                {
                    name: 'Temp 2',
                    data: [
                        {
                            x: 'Design',
                            y: [
                                tickToDate(637122821057885758),
                                    tickToDate(637122822098610580)
                            ]
                        },
                        {
                            x: 'Code',
                            y: [
                                tickToDate(637122823098610580),
                                    tickToDate(637122824098610580)
                            ]
                        },
                        {
                            x: 'Test',
                            y: [
                                tickToDate(637122825098610580),
                                    tickToDate(637122826098610580)
                            ]
                        }
                    ]
                }
            ]
        }
    }

    render() {
        return (


            <div id="chart22">
                <ReactApexChart
                    options={this.state.options} series={this.state.series}
                    type="rangeBar" height="350"/>
            </div>)
    }
}
